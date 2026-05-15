# Spatial `$filter` Translation in Restier (Spec B)

**Date:** 2026-05-15
**Status:** Design draft (awaiting user review)
**Predecessor:** [Spec A — Spatial Types Round-Trip](./2026-05-06-spatial-types-roundtrip-design.md)
**Issue:** [OData/RESTier#673](https://github.com/OData/RESTier/issues/673) (filtering portion)

## Goal

Translate the three OData v4 spatial query functions — `geo.distance`, `geo.length`, `geo.intersects` — into server-side LINQ so they execute as native SQL spatial operators (T-SQL on SQL Server, PostGIS on Npgsql, etc.) rather than 4xx-ing at the OData filter binder. Both Entity Framework 6 (`DbGeography` / `DbGeometry`) and Entity Framework Core (NetTopologySuite) are covered symmetrically. The `$filter=geo.distance(...) lt N` negative integration test shipped in Spec A flips to a positive test, and matching positive coverage is added for `geo.length` and `geo.intersects`.

This is the second of three planned specs. Source-generator/`[SpatialProperty]` sugar for users who prefer Microsoft.Spatial-typed entity properties is deferred to Spec C. Later items — `geo.*` in `$orderby`, default-SRID configuration, `Edm.Stream` for very-large geometries — are explicitly out of scope.

## Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Function coverage | `geo.distance`, `geo.length`, `geo.intersects` | The three v4-core spatial functions. Non-core (`geo.coveredby`, `geo.contains`, ...) fall through to the base `FilterBinder` (HTTP 400) — out of scope. |
| EF flavor symmetry | Both EF6 and EF Core | Mirrors Spec A. EF6's `DbGeography.Distance` and EF Core's NTS `Geometry.Distance` both translate natively under their respective providers, so a single binder works for both via Spec A's `ISpatialTypeConverter` indirection. |
| Mechanism | Custom `IFilterBinder` subclass | AspNetCoreOData's official extension point. Alternatives (post-bind tree rewrite, ODL-level visitor) don't survive: the default `FilterBinder` throws on `geo.*` before any downstream processor sees the expression, and ODL has no node kind to express "call CLR method X". |
| Opt-in surface | Extend `AddRestierSpatial()` to also register the binder | One-line opt-in for round-trip *and* filtering, matching Spec A's UX. `TryAddSingleton` makes double-registration (both EF6 and EF Core spatial packages referenced) safe. |
| Error policy on bad input | Hybrid — happy-path translates, unknown function names fall through, genus / CRS mismatches throw `ODataException` | Translates the three supported functions; preserves AspNetCoreOData's stock "unknown function" error for forward compat with future OData additions; surfaces user errors as `400 Bad Request` with property/function context. |
| Path-segment `$filter` coverage | Fixed too | `RestierQueryBuilder.HandleFilterPathSegment` currently `new FilterBinder()`s with no DI resolution. Spec B switches it to resolve `IFilterBinder` from the request services (default fallback preserved). One mental model for both `?$filter=` and `/$filter(...)` URL shapes. |
| `geo.length` argument validation | Delegated to the provider | OData v4 specifies the input must be `Edm.GeographyLineString` / `Edm.GeometryLineString`. EF6 `DbGeography.Length` returns `null` for non-LineString inputs; NTS `Geometry.Length` returns the boundary length (perimeter for polygons). We don't duplicate the validation at bind time — would require ODL EDM-type plumbing the binder doesn't have. |
| Provider awareness | None | EF6 SQL Server, EF Core SQL Server NTS plugin, and Npgsql PostGIS each provide their own LINQ-to-SQL translation for the storage CLR members. Spec B is a pure binding/expression-shaping concern. |
| CRS handling on literals | Mirror Spec A — fail-fast on non-EPSG | Spec A's `ISpatialTypeConverter.ToStorage` throws `InvalidOperationException` for `CoordinateSystem.EpsgId == null`. The binder catches that specific exception and rewraps as `ODataException` so it flows out as a 400. Consistent posture across read, write, and filter. |

## Background

Spec A made Microsoft.Spatial-typed values round-trip through entity properties typed in the storage library (`DbGeography` / `DbGeometry` for EF6, `NetTopologySuite.Geometries.Geometry`-subclass for EF Core). It deliberately stopped short of any `$filter` translation, asserting the limitation via a negative integration test:

```csharp
// test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs:122-134
[Fact]
public async Task EFCore_Filter_GeoDistance_IsNotTranslatable_ReturnsError()
{
    var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
        HttpMethod.Get,
        resource: "/SpatialPlaces?$filter=geo.distance(HeadquartersLocation,geography'POINT(0 0)') lt 10000",
        serviceCollection: _configureServices);

    response.IsSuccessStatusCode.Should().BeFalse(
        "geo.distance translation is not supported by EF Core + NTS (spec-A limitation); " +
        "the server must return a 4xx or 5xx error, not a successful response");
}
```

The failure today is AspNetCoreOData's default `FilterBinder.BindSingleValueFunctionCallNode` throwing on the `geo.distance` `SingleValueFunctionCallNode` (no handler is registered for the `geo.*` function namespace). Spec B replaces the binder with one that recognises the three v4-core spatial functions and rewrites them as LINQ method/property access against the storage CLR type. EF6's SQL Server provider and EF Core's NTS plugin then translate the LINQ to native SQL spatial operators.

The current main-path `$filter` ingestion goes through `RestierController.ApplyQueryOptions` → `ODataQueryOptions.ApplyTo`, which resolves `IFilterBinder` from the per-request service container — so registering a custom binder in route services is sufficient for the common case. A second code path, `RestierQueryBuilder.HandleFilterPathSegment` (`src/Microsoft.Restier.AspNetCore/Query/RestierQueryBuilder.cs:307-319`), handles path-segment `$filter` syntax (`/SpatialPlaces/$filter(...)`) and currently `new FilterBinder()`s with no DI lookup. Spec B fixes that path too, since leaving it would create a "works in one URL shape, fails in the other" inconsistency for the same query.

## Architecture

### Components

| # | Component | Assembly | Notes |
|---|-----------|----------|-------|
| 1 | `RestierSpatialFilterBinder` (subclass of `Microsoft.AspNetCore.OData.Query.Expressions.FilterBinder`) | `Microsoft.Restier.AspNetCore` | Stateless. Ctor accepts `IEnumerable<ISpatialTypeConverter>` (Spec A primitive). Overrides `BindSingleValueFunctionCallNode`. |
| 2 | `RestierQueryBuilder.HandleFilterPathSegment` refactor | `Microsoft.Restier.AspNetCore` | Resolves `IFilterBinder` from the request services, falls back to `new FilterBinder()` when nothing's registered. Restores parity with the main `$filter` path. |
| 3 | `AddRestierSpatial()` extension update — both flavors | `Microsoft.Restier.EntityFramework.Spatial`, `Microsoft.Restier.EntityFrameworkCore.Spatial` | Adds `services.TryAddSingleton<IFilterBinder, RestierSpatialFilterBinder>()`. `TryAdd` keeps double-registration safe if both packages are referenced. |
| 4 | Two new resource strings | `Microsoft.Restier.AspNetCore` | `SpatialFilter_GenusMismatch`, `SpatialFilter_NoConverterForStorageType`. Both used only inside Spec B's dispatch arms. |

### Why one binder for both flavors

The binder doesn't need to know whether the storage type is `DbGeography`, `DbGeometry`, or an NTS subclass. The three things it cares about are:

1. **What storage CLR type does the bound property argument have?** This comes straight off the bound `Expression.Type` after `base.Bind(arg)` — Spec A's model-builder convention preserves the storage-typed CLR property and only substitutes EDM-side, so `Expression.Type` is `DbGeography` / `DbGeometry` / `NTS.Point` / etc. depending on the flavor.
2. **What's the storage value for the spatial literal?** Resolved by asking each registered `ISpatialTypeConverter` whose `CanConvert(storageType)` is true to `ToStorage(storageType, edmValue)`. Spec A registers exactly one converter per flavor, so the answer is unambiguous.
3. **What CLR member do I call?** `Distance` / `Length` / `Intersects` are present (with matching signatures) on every supported storage type — looked up reflectively via `Expression.Call` / `Expression.Property` against the bound argument's runtime CLR type. No flavor switch needed.

### Component placement

`RestierSpatialFilterBinder` lives in `Microsoft.Restier.AspNetCore` even though both `.Spatial` packages register it. Rationale:

- The binder references `Microsoft.AspNetCore.OData.Query.Expressions.FilterBinder` and `ISpatialTypeConverter` only — no EF6 or EF Core types. Putting it in `Microsoft.Restier.AspNetCore` (which already references AspNetCoreOData) avoids any EF flavor leakage in core wiring.
- DI registration stays in the `.Spatial` packages. Consumers who don't reference either package don't have the class in their composition root and the default `FilterBinder` is used as before — no behavior change for non-spatial APIs.

### `RestierSpatialFilterBinder` shape

```csharp
namespace Microsoft.Restier.AspNetCore.Query
{
    public class RestierSpatialFilterBinder : FilterBinder
    {
        private readonly ISpatialTypeConverter[] converters;

        public RestierSpatialFilterBinder(IEnumerable<ISpatialTypeConverter> converters = null)
        {
            this.converters = converters?.ToArray() ?? Array.Empty<ISpatialTypeConverter>();
        }

        public override Expression BindSingleValueFunctionCallNode(
            SingleValueFunctionCallNode node, QueryBinderContext context)
        {
            switch (node.Name)
            {
                case "geo.distance":   return BindGeoDistance(node, context);
                case "geo.length":     return BindGeoLength(node, context);
                case "geo.intersects": return BindGeoIntersects(node, context);
                default:               return base.BindSingleValueFunctionCallNode(node, context);
            }
        }

        // BindGeo* helpers: bind each child via base.Bind, lower spatial-literal
        // ConstantExpressions to storage values via the converters, then Expression.Call
        // / Expression.Property on the storage CLR type.
    }
}
```

The `converters = null` default keeps the parameterless construction working for the (rare) case where someone hand-builds the binder outside DI — the base behavior is preserved.

### `HandleFilterPathSegment` change

Before (`src/Microsoft.Restier.AspNetCore/Query/RestierQueryBuilder.cs:307-319`):

```csharp
var filterBinder = new FilterBinder();
queryable = filterBinder.ApplyBind(queryable, filterClause, context);
```

After (sketch):

```csharp
var filterBinder = serviceProvider.GetService<IFilterBinder>() ?? new FilterBinder();
queryable = filterBinder.ApplyBind(queryable, filterClause, context);
```

`serviceProvider` is already available in the surrounding `RestierQueryBuilder` class; the precise expression resolves it the same way other path-segment handlers resolve services. If `IFilterBinder` isn't registered, the new code is observationally identical to the old code — no behavior change for non-spatial APIs.

### `AddRestierSpatial()` change (both flavors)

Each flavor's `Extensions/ServiceCollectionExtensions.cs` gains one line in `AddRestierSpatial(this IServiceCollection services)`:

```csharp
services.TryAddSingleton<IFilterBinder, RestierSpatialFilterBinder>();
```

`TryAdd` ensures the second-registration scenario (both `.Spatial` packages referenced) is a no-op rather than overwriting an existing registration. The binder's ctor pulls `IEnumerable<ISpatialTypeConverter>` from DI, so whichever flavor's converters are registered get used.

### Data flow per function

Each dispatch arm follows the same three-step pattern: bind children → lower spatial literals → emit storage-typed member access. Detailed below.

#### `geo.distance(arg0, arg1)`

1. `boundArg0 = base.Bind(node.Parameters[0], context)`; same for `boundArg1`. After this step, the property-side bound expression has `Expression.Type` equal to the storage CLR type (`DbGeography` etc.); the literal-side bound expression is a `ConstantExpression` whose `Value` is a `Microsoft.Spatial.Geography*` / `Geometry*` runtime instance.
2. **Lower the spatial literal.** For each bound expression whose `Value`'s runtime type is a `Microsoft.Spatial` subclass:
   - Take the "other" argument's `Expression.Type` as the storage target type.
   - Find a `converter` whose `CanConvert(storageTargetType)` returns `true`.
   - Replace the `ConstantExpression` with `Expression.Constant(converter.ToStorage(storageTargetType, edmValue), storageTargetType)`.
   - If no converter matches, throw `ODataException` (`SpatialFilter_NoConverterForStorageType`, see Error handling).
   - If `ToStorage` throws `InvalidOperationException` (Spec A's non-EPSG fail-fast path), wrap it in `ODataException` preserving the original message.
3. **Emit** `Expression.Call(storageArg0, storageArg0.Type.GetMethod("Distance", new[] { storageArg1.Type }), storageArg1)`.

The returned `MethodCallExpression`'s type is `double?` (EF6) or `double` (NTS). Base `FilterBinder.BindBinaryOperatorNode` handles the `lt N` wrapper — including nullable-versus-non-nullable comparison — without any further intervention.

#### `geo.length(arg0)`

1. Bind `boundArg0` via base.
2. **Emit** `Expression.Property(boundArg0, "Length")`. `DbGeography.Length` and `DbGeometry.Length` are `double?` instance properties; `NTS.Geometry.Length` is a `double` instance property. Reflection-based resolution against `boundArg0.Type` returns the right `PropertyInfo` automatically.

No literal lowering — `geo.length` is unary.

#### `geo.intersects(arg0, arg1)`

Same shape as `geo.distance`. Lower spatial literals, then `Expression.Call(storageArg0, "Intersects", typeArguments: null, storageArg1)`. Return type is `bool?` (EF6) or `bool` (NTS); base bind handles the predicate position.

#### Cross-cutting

- **Literal-on-literal calls** (`geo.distance(geography'...', geography'...')`) are uncommon but legal. The binder handles them symmetrically: when both arguments are literals, the first one's storage type seeds the lowering of the second. If neither side has a determinable storage type (no property-access node anywhere in the call), it falls through to the no-converter error path.
- **Genus matching.** Spec A's `ISpatialTypeConverter` contract: `CanConvert(targetStorageType)` plus a runtime-genus check inside `ToStorage`. A `GeometryPoint` literal against a `DbGeography` property fails one of those checks; we surface that as the `SpatialFilter_GenusMismatch` error described in § Error handling.
- **Null property values.** EF6 and EF Core both propagate null through instance-method spatial calls — `null.Distance(x) → null` in three-valued SQL logic. The binder relies on that and adds no special-case handling.
- **Provider translation.** EF6's SQL Server provider, EF Core's SQL Server NTS plugin, and Npgsql's PostGIS plugin each translate `DbGeography`/`Geometry`/`NTS.Geometry` instance members to native SQL spatial operators. Spec B contributes zero provider-aware code; if the configured provider can't translate, the failure surfaces from EF as it would for any unsupported LINQ call.

## Error handling

Three concrete cases. All `ODataException`s map to HTTP 400 via AspNetCoreOData's stock exception handler.

### Unknown `geo.*` function name

Cases: `geo.area`, `geo.contains`, `geo.coveredby`, `geo.within`, anything else not in OData v4 core.

- **Action:** fall through to `base.BindSingleValueFunctionCallNode(node, context)`.
- **Surface:** AspNetCoreOData's default produces an `ODataException` of the form *"An unknown function with name 'geo.area' was found. This may also be a function imported in a service…"* → HTTP 400. Existing behavior; not shadowed.

### Genus mismatch

Cases: `geo.distance(GeographyProperty, geometry'...')`, two literals of incompatible families, or any other path where no registered converter's `CanConvert` matches the storage target type's genus against the literal's genus.

- **Action:** throw `new ODataException(Resources.SpatialFilter_GenusMismatch.FormatWith(functionName, propertyPath, propertyGenus, literalGenus))`.
- **Surface:** HTTP 400. Example: *"Cannot bind 'geo.distance' on 'HeadquartersLocation' (Geography) against a Geometry literal."*

### Non-EPSG / unsupported CRS

Case: parsed `Microsoft.Spatial.CoordinateSystem.EpsgId == null` on a literal.

- **Detection:** Spec A's `ISpatialTypeConverter.ToStorage` already throws `InvalidOperationException` for this — see Spec A § "Non-EPSG coordinate systems".
- **Action:** the binder wraps the `InvalidOperationException` thrown by the converter call in an `ODataException`, preserving the original message and adding the function/property context.
- **Surface:** HTTP 400. Same posture as Spec A's submit-time rejection of non-EPSG CRS, applied at filter-bind time.

### Catch scope

The wrap-as-`ODataException` happens only inside the three dispatch arms, around the converter call and the literal-lowering step. The binder never catches around `base.BindSingleValueFunctionCallNode(node, context)` or around `base.Bind(arg, context)` — masking those would swallow legitimate parse errors.

### What is deliberately not validated

- `geo.length`'s argument is not type-checked at bind time. EF6 returns null for non-LineString; NTS returns boundary length. See the decision table.
- The literal's SRID is not validated against an allowlist — the only check is "has non-null `EpsgId`", which is Spec A's contract.

### Resource strings

In `src/Microsoft.Restier.AspNetCore/Properties/Resources.resx` (and the generated `Resources.Designer.cs`):

- `SpatialFilter_GenusMismatch` — placeholders: function name, property name, property genus, literal genus.
- `SpatialFilter_NoConverterForStorageType` — placeholders: function name, property name, storage type. Surfaces when `geo.*` is called against a spatial property but no converter is registered for that storage type (e.g., `AddRestierSpatial()` was forgotten or the wrong-flavor package is referenced).

## Test plan

### Unit tests — new file `test/Microsoft.Restier.Tests.AspNetCore/Query/RestierSpatialFilterBinderTests.cs`

Each test constructs a `FilterClause` via `ODataQueryOptionParser`, calls `binder.ApplyBind(IQueryable, filterClause, context)`, and asserts on the resulting LINQ `Expression`. No DB roundtrip, no HTTP — fast.

Each positive case runs as an xUnit theory across the two flavor converters (`DbSpatialConverter`, `NtsSpatialConverter`) to prove the binder is converter-agnostic.

- `BindSingleValueFunctionCallNode_GeoDistance_EmitsStorageDistanceMethodCall` — `geo.distance(prop, literal) lt N` → expression tree shape: `MethodCallExpression(prop, "Distance", [storageLiteral]) < ConstantExpression(N)`.
- `BindSingleValueFunctionCallNode_GeoLength_EmitsStorageLengthProperty` — shape: `MemberExpression(prop, "Length") > ConstantExpression(0)`.
- `BindSingleValueFunctionCallNode_GeoIntersects_EmitsStorageIntersectsMethodCall` — shape: `MethodCallExpression(prop, "Intersects", [storageLiteral])`.
- `BindSingleValueFunctionCallNode_UnknownGeoFunction_FallsThroughToBase` — `geo.unknown(prop, literal)` → `ODataException` whose message contains `"unknown function"`. Proves fall-through preserves AspNetCoreOData's error.
- `BindSingleValueFunctionCallNode_GenusMismatch_ThrowsODataException` — `geo.distance(HeadquartersLocation, geometry'POINT(0 0)')` against a `DbGeography` property → `ODataException` containing both genus names.
- `BindSingleValueFunctionCallNode_NonEpsgLiteral_WrapsInvalidOperationAsODataException` — `geo.distance(prop, geography'SRID=99999;POINT(0 0)')` → `ODataException` whose `InnerException` is `InvalidOperationException` and whose message contains the property name and SRID.
- `Ctor_NoConvertersRegistered_GeoFunctionThrowsNoConverterError` — binder built with an empty converter enumerable + `geo.distance(...)` against a spatial property → `ODataException` matching `SpatialFilter_NoConverterForStorageType`.

### Library scenario extension

`test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/SpatialPlace.cs` gains a `RouteLine` property (under the existing `#if EF6` / `#if EFCore` blocks):

```csharp
#if EF6
public DbGeography RouteLine { get; set; }
#endif
#if EFCore
public NetTopologySuite.Geometries.LineString RouteLine { get; set; }
#endif
```

`LibraryTestInitializer.cs` (both flavor copies) seeds `LINESTRING(0 0, 1 1, 2 2)` with SRID 4326 on each row. This gives `geo.length` a LineString target — neither the existing `HeadquartersLocation` (Point) nor `ServiceArea` (Polygon) is a valid LineString input.

### Integration tests — `test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs`

**Flip the existing negative test.** `EFCore_Filter_GeoDistance_IsNotTranslatable_ReturnsError` is renamed to `EFCore_Filter_GeoDistance_TranslatesAndReturnsSeededRow` and changes from `IsSuccessStatusCode.Should().BeFalse()` to asserting 200 OK plus the seeded Amsterdam row in the result set.

**New positive tests — each in both `_EF6` and `_EFCore` flavors** (eight tests total):

- `Filter_GeoDistance_TranslatesAgainstStorageProperty` — `?$filter=geo.distance(HeadquartersLocation,geography'SRID=4326;POINT(0 0)') lt 10000000`. Asserts 200 + body contains the Amsterdam-seeded row.
- `Filter_GeoLength_TranslatesPropertyAccess` — `?$filter=geo.length(RouteLine) gt 0`. Asserts 200 + non-empty result set.
- `Filter_GeoIntersects_TranslatesMethodCall` — `?$filter=geo.intersects(ServiceArea,geography'SRID=4326;POINT(0.5 0.5)')`. Asserts 200 + the row whose polygon contains the test point.
- `Filter_GeoDistance_PathSegmentSyntax_TranslatesToo` — `/SpatialPlaces/$filter(geo.distance(...) lt N)`. Same payload assertion, exercises the `HandleFilterPathSegment` change.

**New negative tests — flavor-agnostic where possible** (four tests):

- `Filter_GeoDistance_GenusMismatch_Returns400` — Geography property vs `geometry'...'` literal. Asserts 400 and that the body mentions the property and both genera.
- `Filter_GeoDistance_NonEpsgSrid_Returns400` — `geography'SRID=99999;POINT(0 0)'` literal. Asserts 400 and that the body preserves the Spec-A non-EPSG message.
- `Filter_GeoArea_UnknownFunction_Returns400` — proves fall-through preserves AspNetCoreOData's error message.
- `Filter_GeoDistance_WithoutAddRestierSpatial_Returns400` — bootstraps an API without `AddRestierSpatial()`; asserts the legacy error mode is unchanged (still 400, original AspNetCoreOData message).

### `RestierQueryBuilder` unit coverage

A new test class in `test/Microsoft.Restier.Tests.AspNetCore/Query/` confirms that `HandleFilterPathSegment` resolves a DI-registered `IFilterBinder` when one is present, and falls back to `new FilterBinder()` when nothing's registered. Regression-protects the change.

### Baselines

No metadata baseline regeneration. Spec B doesn't change the EDM model — only filter semantics. All assertions are on JSON response bodies.

## Documentation

`src/Microsoft.Restier.Docs/guides/extending-restier/spatial-types.mdx` updates:

- Remove the `geo.*` translation entry from "What's not yet supported" (line 126 today).
- Add a new top-level section "Server-side filtering with `geo.*` functions" above "How it works", listing the supported function matrix, an example query against the Library scenario, the genus-mismatch and non-EPSG error notes, and a forward pointer to "later" items (`$orderby`, default-SRID config).

The docsproj regenerates `docs.json` on build; commit the regenerated file alongside the `.mdx` change.

## Sample app

Optional. `src/Microsoft.Restier.Samples.Postgres.AspNetCore/README.md` gains a "Try a spatial filter" snippet showing `Users?$filter=geo.distance(HomeLocation,geography'SRID=4326;POINT(4.9 52.4)') lt 500000`. No code change in the sample — the existing `AddRestierSpatial()` call lights up filtering automatically once Spec B ships. If this is too far from Spec B's core surface for your taste, skip it.

EF6 sample is unchanged.

## Scope

### Source files added

- `src/Microsoft.Restier.AspNetCore/Query/RestierSpatialFilterBinder.cs`

### Source files modified

- `src/Microsoft.Restier.AspNetCore/Query/RestierQueryBuilder.cs` — `HandleFilterPathSegment` resolves `IFilterBinder` from request services with a default fallback.
- `src/Microsoft.Restier.AspNetCore/Properties/Resources.resx` (+ generated `Resources.Designer.cs`) — two new strings.
- `src/Microsoft.Restier.EntityFramework.Spatial/Extensions/ServiceCollectionExtensions.cs` — `TryAddSingleton<IFilterBinder, RestierSpatialFilterBinder>()` inside `AddRestierSpatial`.
- `src/Microsoft.Restier.EntityFrameworkCore.Spatial/Extensions/ServiceCollectionExtensions.cs` — same line.

### Test files

- `test/Microsoft.Restier.Tests.AspNetCore/Query/RestierSpatialFilterBinderTests.cs` — new, unit tests.
- `test/Microsoft.Restier.Tests.AspNetCore/Query/RestierQueryBuilderFilterBinderResolutionTests.cs` — new, regression test for the DI-resolution change.
- `test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs` — flip existing negative, add eight positive + four negative cases.
- `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/SpatialPlace.cs` — add `RouteLine`.
- `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryTestInitializer.cs` — seed `RouteLine` (EF6 path).
- `test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Scenarios/Library/LibraryTestInitializer.cs` — seed `RouteLine` (EF Core path).

### Documentation files

- `src/Microsoft.Restier.Docs/guides/extending-restier/spatial-types.mdx` — remove `geo.*` entry from "What's not yet supported", add new "Server-side filtering with `geo.*` functions" section.
- `src/Microsoft.Restier.Docs/docs.json` — regenerated by docsproj build.

### Sample app changes

- `src/Microsoft.Restier.Samples.Postgres.AspNetCore/README.md` — optional example query.

### Solution

- `RESTier.slnx` — no project additions. All work lands in existing projects.

### Not changed in Spec B

- `Microsoft.Restier.Core` — no new abstractions. `ISpatialTypeConverter` is reused as-is from Spec A.
- The model-builder convention from Spec A — Spec B operates entirely on the bound LINQ expression tree; no EDM-model changes.
- `RestierPayloadValueConverter` — read path unchanged.
- `EFChangeSetInitializer` (both flavors) — write path unchanged.

## Out of scope (deferred)

| Deferred to | Item |
|-------------|------|
| Spec C | Source-generator or convention-driven sugar for users who prefer Microsoft.Spatial-typed entity properties (inverse of Spec A's storage-typed model). |
| Later | `$orderby=geo.distance(...)` translation. |
| Later | Default-SRID configuration (per-API or per-property). The fail-fast non-EPSG behavior from Spec A is preserved by Spec B without a configurable default. |
| Later | `Edm.Stream` for very-large geometries. |
| Later | OData v4 spatial functions beyond the core three (`geo.coveredby`, `geo.contains`, `geo.within`, `geo.area`, ...) — currently fall through to the base binder (HTTP 400). |
| Later | Provider-specific spatial extension methods (NTS `IsValid`, PostGIS `ST_*`, etc.) exposed via OData function imports. |
