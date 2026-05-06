# Spatial Types Round-Trip in Restier (Spec A)

**Date:** 2026-05-06
**Status:** Design approved
**Issue:** [OData/RESTier#673](https://github.com/OData/RESTier/issues/673)

## Goal

Add round-trip support for Microsoft.Spatial geographic and geometric types in Restier across **both** Entity Framework 6 and Entity Framework Core. Users declare a single property typed in the storage library (`DbGeography`/`DbGeometry` for EF6, `NetTopologySuite.Geometries.Geometry` and subclasses for EF Core); Restier exposes it to OData clients as the corresponding `Edm.Geography*` / `Edm.Geometry*` primitive and converts in both directions transparently.

This is the first of three planned specs. Server-side spatial filtering (`geo.distance`, `geo.length`, etc.) and entity-property sugar (e.g. a `[SpatialProperty]` source generator) are deferred to follow-up specs B and C respectively.

## Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Entity shape | Single property typed in the storage library | Avoids the dual-property pattern (`Location` + `EdmLocation`) the bytefish reference uses; keeps user code free of OData concerns and positions spec B's filter binder cleanly because the LINQ expression `e.Location` is already the EF-native type. |
| EF6 + EF Core symmetry | Both supported with the same `ISpatialTypeConverter` interface | Issue #673 explicitly calls for EF Core support and "a nice interface for abstraction". |
| Type families | Geography and Geometry both | Plumbing is genus-agnostic; covering both now avoids a future migration for users with `DbGeometry` / projected NTS columns. |
| EF6 type disambiguation | Default to `Edm.Geography` / `Edm.Geometry` (abstract base); optional `[Spatial(typeof(GeographyPoint))]` attribute for precision | Zero-config story works out of the box; opt-in precision when schema strictness matters. |
| Package layout | Two new optional packages: `Microsoft.Restier.EntityFramework.Spatial` and `Microsoft.Restier.EntityFrameworkCore.Spatial` | Mirrors the `Microsoft.Restier.AspNetCore.Swagger` precedent. NetTopologySuite (~3 MB plus transitive deps) is opt-in instead of forced on every Restier-EFCore consumer. |
| WKT bridge | Microsoft.Spatial's `WellKnownTextSqlFormatter` paired with `DbGeography.AsText`/`FromText` and NTS's `WKTReader`/`WKTWriter` | Single, well-tested round-trip path covering all Microsoft.Spatial types (Point, LineString, Polygon, MultiPoint, MultiLineString, MultiPolygon, Collection). Replaces the current hand-built WKT in `GeographyConverter`, which has an axis-order bug and a Latitude/Latitude mix-up in `ToGeographyLineString`. |
| Filter-binder behavior | Spec A explicitly does **not** ship `geo.*` translation. Tests assert the limitation. | Round-trip and filtering are separable concerns; landing them together inflates spec scope. Spec B picks up filtering on top of A's foundation. |

## Background

OData V4 declares `Edm.Geography*` and `Edm.Geometry*` as primitive types and Microsoft.Spatial provides their CLR representations. Neither EF6 nor EF Core natively maps Microsoft.Spatial types to database columns:

- **EF6** maps spatial columns to `System.Data.Entity.Spatial.DbGeography` / `DbGeometry`. There is no value-converter mechanism in EF6 to substitute a different CLR type.
- **EF Core** with the SQL Server NTS plugin (or PostGIS via Npgsql) maps spatial columns to `NetTopologySuite.Geometries.Geometry` and concrete subclasses (`Point`, `Polygon`, etc.).

The current `feature/vnext` branch ships a partial EF6-only implementation in `src/Microsoft.Restier.EntityFramework/Spatial/GeographyConverter.cs` that hand-builds WKT strings for `Point` and `LineString` only. The implementation has known bugs (WKT axis order is `lat lon` instead of `lon lat`; `ToGeographyLineString` reads `point.Latitude` for both arguments of `GeographyPosition`) and only the inbound (write) path is wired into `EFChangeSetInitializer.ConvertToEfValue`. The outbound (read) path is not wired anywhere, so reads of `DbGeography` columns currently fail to serialize. There is no EF Core support whatsoever.

The bytefish reference implementation linked from issue #673 ([www.bytefish.de](https://www.bytefish.de/blog/aspnet_core_odata_example.html#extending-partial-classes-with-microsoftspatial-properties)) demonstrates a working EF Core approach using a dual-property pattern (one NTS-typed property for EF, a partial-class shadow Microsoft.Spatial-typed property for OData) plus a model-builder rename hook. Spec A takes a different path — single property, pipeline conversions — for the reasons captured in the Decisions table.

## Architecture

### Components

Five components, distributed across the existing assembly layout plus two new optional packages:

| # | Component | Assembly | Notes |
|---|-----------|----------|-------|
| 1 | `[SpatialAttribute]`, `ISpatialTypeConverter` interface | `Microsoft.Restier.Core` | No new dependencies. Microsoft.Spatial is already transitive via Microsoft.OData.Edm. |
| 2 | EDM model-builder convention | `Microsoft.Restier.AspNetCore` | Lifts storage-typed properties into Microsoft.Spatial-typed EDM properties at model-build time. |
| 3 | Read-path hook in `RestierPayloadValueConverter` | `Microsoft.Restier.AspNetCore` | Same pattern as the DateOnly outbound conversion. |
| 4 | Write-path hook in each flavor's `EFChangeSetInitializer.ConvertToEfValue` | `Microsoft.Restier.EntityFramework`, `Microsoft.Restier.EntityFrameworkCore` | Replaces the existing EF6 `DbGeography` branch with a converter dispatch; adds the symmetric branch to EF Core. |
| 5 | Per-flavor converter implementations | new packages `Microsoft.Restier.EntityFramework.Spatial`, `Microsoft.Restier.EntityFrameworkCore.Spatial` | Registered into the route service container via `services.AddRestierSpatial()`. |

### `[Spatial]` attribute and `ISpatialTypeConverter` interface

In `Microsoft.Restier.Core/Spatial/`:

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class SpatialAttribute : Attribute
{
    public SpatialAttribute(Type edmType) { EdmType = edmType; }
    public Type EdmType { get; }      // a Microsoft.Spatial.Geography* or Geometry* CLR type
}

public interface ISpatialTypeConverter
{
    bool CanConvert(Type storageType);
    object ToEdm(object storageValue, Type targetEdmType);
    object ToStorage(Type targetStorageType, object edmValue);
}
```

`CanConvert` lets multiple converters coexist in DI (one per EF flavor); the resolver picks the first registered converter whose `CanConvert` returns true for the value's storage type. Both `ToEdm` and `ToStorage` take an explicit target type because:
- `ToEdm` from `DbGeography` cannot infer `GeographyPoint` vs `GeographyPolygon` without external context — the EDM model-build step or the runtime EDM type reference supplies it.
- `ToStorage` needs the property's declared CLR type to round-trip into `DbGeography` vs `DbGeometry` (or the appropriate NTS subclass).

### EDM model-builder convention

A new `SpatialModelConvention` in `Microsoft.Restier.AspNetCore/Model/` runs during model-build. For each entity property:

1. If the CLR type is `DbGeography`, `DbGeometry`, `NetTopologySuite.Geometries.Geometry`, or any subclass:
   - **`[Spatial]` attribute present** → use its `EdmType`.
   - **No attribute, EF6** →
     - `DbGeography` → `Edm.Geography` (abstract base)
     - `DbGeometry` → `Edm.Geometry` (abstract base)
   - **No attribute, EF Core** →
     - NTS concrete subclass (`Point`/`Polygon`/etc.) → matching `Edm.GeographyPoint`/`Edm.GeographyPolygon`/etc., **Geography assumed**
     - NTS abstract `Geometry` → `Edm.Geography`
     - **Geometry override**: an `ISpatialModelMetadataProvider` from the EFCore.Spatial package consults the EFCore mutable model (`IEntityType.FindProperty(...).GetColumnType()`); if the column type is `"geometry"` (or vendor variant), swap from Geography to Geometry.
2. Register the EDM property by passing the **resolved Microsoft.Spatial CLR type** (from step 1) to `EdmHelpers.GetPrimitiveTypeKind`, which is extended in this spec to recognize Microsoft.Spatial types and return the correct `EdmPrimitiveTypeKind.Geography*` / `Geometry*` value. Note: the entity's storage-typed property (`DbGeography`, NTS `Point`, etc.) is never passed to `GetPrimitiveTypeKind` directly — the convention always substitutes the Microsoft.Spatial type before calling it.

The convention itself only references CLR `Type`s. EF6- and EFCore-specific knowledge (storage-type recognition, EFCore column-type lookup) lives behind the `ISpatialModelMetadataProvider` interface so the AspNetCore project keeps its current dependency graph.

### Read-path hook

Extend `RestierPayloadValueConverter.ConvertToPayloadValue` with a spatial branch:

```csharp
if (edmTypeReference is not null && IsSpatialEdmType(edmTypeReference) && value is not null)
{
    var converter = ResolveSpatialConverter(value.GetType());
    if (converter is not null)
    {
        var targetClrType = MapEdmSpatialToClr(edmTypeReference);
        return converter.ToEdm(value, targetClrType);
    }
}

return base.ConvertToPayloadValue(value, edmTypeReference);
```

`ResolveSpatialConverter` reads `IEnumerable<ISpatialTypeConverter>` from the route service container and picks the first match by `CanConvert`, cached by storage type. Microsoft.Spatial values that arrive at the converter (because the user declared a Microsoft.Spatial-typed property directly) flow through unchanged via `base`.

### Write-path hook

In **both** `EFChangeSetInitializer.ConvertToEfValue` implementations, replace the existing spatial branch with a converter dispatch:

```csharp
// EF6
if (typeof(DbGeography).IsAssignableFrom(type) || typeof(DbGeometry).IsAssignableFrom(type))
{
    var converter = SpatialConverters.Resolve(type);
    if (converter is not null && value is not null)
    {
        return converter.ToStorage(type, value);
    }
}

// EF Core (new)
if (typeof(NetTopologySuite.Geometries.Geometry).IsAssignableFrom(type))
{
    var converter = SpatialConverters.Resolve(type);
    if (converter is not null && value is not null)
    {
        return converter.ToStorage(type, value);
    }
}
```

`SpatialConverters.Resolve` is a thin static facade over the same DI lookup the read-path uses, reaching the route service container via `SubmitContext.Api`'s domain services.

### Per-flavor converter packages

Two new `csproj`s ship the converter implementations:

```
src/Microsoft.Restier.EntityFramework.Spatial/
    DbSpatialConverter.cs
    Extensions/ServiceCollectionExtensions.cs            (AddRestierSpatial)

src/Microsoft.Restier.EntityFrameworkCore.Spatial/
    NtsSpatialConverter.cs
    EFCoreSpatialModelMetadataProvider.cs                (HasColumnType lookup)
    Extensions/ServiceCollectionExtensions.cs            (AddRestierSpatial)
```

Both converters use the same WKT round-trip algorithm:
- `ToEdm`: storage value → WKT (via `DbGeography.AsText()` / `NTS.WKTWriter`) → Microsoft.Spatial instance (via `WellKnownTextSqlFormatter.Read<TGeo>(reader)`).
- `ToStorage`: Microsoft.Spatial → WKT (via `WellKnownTextSqlFormatter.Write(value, writer)`) → storage value (`DbGeography.FromText` / `NTS.WKTReader.Read`).

User-facing wiring is a single line per project:

```csharp
services.AddRestierSpatial();
```

### Data flow summary

**Read** (DB → client): EF materializes `DbGeography`/`Point` → `RestierPayloadValueConverter.ConvertToPayloadValue` resolves converter → `ToEdm(value, edmType)` → Microsoft.Spatial value → OData writes payload.

**Write** (client → DB): OData deserializes `Edm.Geography*` payload to Microsoft.Spatial value → `ChangeSetInitializer` builds `DataModificationItem` → `ConvertToEfValue(propertyType, value)` resolves converter → `ToStorage(propertyType, value)` → storage value assigned to entity property → EF persists.

## Test plan

### Test scenario integration

Add spatial properties to the **Library** scenario (used by the DateOnly/TimeOnly spec — same pattern, same baselines already maintained), conditional per flavor:

```csharp
// test/Microsoft.Restier.Tests.Shared/Scenarios/Library/Publisher.cs
public class Publisher
{
    public string Id { get; set; }
    public string Name { get; set; }

#if EF6
    public DbGeography HeadquartersLocation { get; set; }       // -> Edm.Geography (abstract base)

    [Spatial(typeof(GeographyPolygon))]
    public DbGeography ServiceArea { get; set; }                 // -> Edm.GeographyPolygon

    public DbGeometry FloorPlan { get; set; }                    // -> Edm.Geometry (abstract base)
#endif

#if EFCore
    public NetTopologySuite.Geometries.Point HeadquartersLocation { get; set; }     // -> Edm.GeographyPoint
    public NetTopologySuite.Geometries.Polygon ServiceArea { get; set; }            // -> Edm.GeographyPolygon

    [Spatial(typeof(GeometryPoint))]
    public NetTopologySuite.Geometries.Point IndoorOrigin { get; set; }             // -> Edm.GeometryPoint (attribute override)
#endif
}
```

`LibraryTestInitializer` seeds well-known points/polygons so round-trip equality is easy to assert.

### Unit tests

Per `.Spatial` package:
- WKT round-trip for every Microsoft.Spatial type (Point, LineString, Polygon, MultiPoint, MultiLineString, MultiPolygon, Collection — Geography and Geometry families).
- Null handling (storage `null` → Edm `null` and back).
- Type mismatch error path (e.g. asking `ToStorage` for `DbGeometry` with a `GeographyPoint` value).
- Axis-order regression: a fix-locked test that fails if anyone reintroduces `lat lon` ordering.

### Integration tests

In `Microsoft.Restier.Tests.AspNetCore`, both flavors:
- `GET /Publishers(1)` returns `Edm.GeographyPoint`/`Edm.GeographyPolygon` payloads with the seeded coordinates.
- `POST /Publishers` with a Microsoft.Spatial Polygon payload persists round-trippable WKT.
- `PATCH /Publishers(1)` updating `HeadquartersLocation` only — exercises the change-set initializer write path.
- `$select=HeadquartersLocation,ServiceArea` returns just the spatial properties.
- `$filter=geo.distance(HeadquartersLocation, ...) lt 10000` — **negative test** asserting the documented spec-A limitation; flips to a positive test in spec B.

### Baseline regenerations

- `LibraryApi-EF6-ApiMetadata.txt` and `LibraryApi-EFCore-ApiMetadata.txt` — the EDM document grows with the new spatial properties. EF6 baseline shows `Edm.Geography` (abstract) for unannotated properties and `Edm.GeographyPolygon` for the attributed property.

## Documentation

- New hand-written guide page `src/Microsoft.Restier.Docs/guides/spatial-types.mdx` covering: which packages to install, what an entity property looks like for each EF flavor, when to use `[Spatial]`, and a "what's not yet supported" note pointing forward to spec B.
- The docsproj `<MintlifyTemplate>` block gets a new entry under the Guides group; `docs.json` regenerates on build.

## Sample app

The Postgres sample (`src/Microsoft.Restier.Samples.Postgres.AspNetCore`) is PostGIS-capable. Add a single spatial property on the `User` entity (e.g. `Point HomeLocation`) plus a migration. Demonstrates the EF Core path end-to-end against a real database.

EF6 sample is unchanged in spec A.

## Scope

### Source files added

- `src/Microsoft.Restier.Core/Spatial/SpatialAttribute.cs`
- `src/Microsoft.Restier.Core/Spatial/ISpatialTypeConverter.cs`
- `src/Microsoft.Restier.Core/Spatial/ISpatialModelMetadataProvider.cs`
- `src/Microsoft.Restier.AspNetCore/Model/SpatialModelConvention.cs`
- `src/Microsoft.Restier.EntityFramework.Spatial/` — new `csproj`, `DbSpatialConverter.cs`, `ServiceCollectionExtensions.cs`
- `src/Microsoft.Restier.EntityFrameworkCore.Spatial/` — new `csproj`, `NtsSpatialConverter.cs`, `EFCoreSpatialModelMetadataProvider.cs`, `ServiceCollectionExtensions.cs`

### Source files modified

- `src/Microsoft.Restier.AspNetCore/Model/EdmHelpers.cs` — extend `GetPrimitiveTypeKind` for Microsoft.Spatial types
- `src/Microsoft.Restier.AspNetCore/RestierPayloadValueConverter.cs` — add spatial branch
- `src/Microsoft.Restier.EntityFramework/Submit/EFChangeSetInitializer.cs` — replace existing `DbGeography` block with converter dispatch
- `src/Microsoft.Restier.EntityFrameworkCore/Submit/EFChangeSetInitializer.cs` — add converter dispatch branch

### Source files deleted

- `src/Microsoft.Restier.EntityFramework/Spatial/GeographyConverter.cs` (and the matching resource strings in `Resources.resx` / `Resources.Designer.cs`)
- Any tests that pin the deleted converter's hand-built WKT behavior

### Test files

- `test/Microsoft.Restier.Tests.EntityFramework.Spatial/` — new project, mirrors the new `.Spatial` package
- `test/Microsoft.Restier.Tests.EntityFrameworkCore.Spatial/` — new project, mirrors the new `.Spatial` package
- `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/Publisher.cs` — conditional spatial properties
- `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryTestInitializer.cs` — seed spatial values
- `test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Scenarios/Library/LibraryTestInitializer.cs` — seed spatial values
- `test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs` — new integration coverage
- `test/Microsoft.Restier.Tests.AspNetCore/Baselines/LibraryApi-EF6-ApiMetadata.txt` — regenerated
- `test/Microsoft.Restier.Tests.AspNetCore/Baselines/LibraryApi-EFCore-ApiMetadata.txt` — regenerated

### Documentation files

- `src/Microsoft.Restier.Docs/guides/spatial-types.mdx` (new)
- `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj` — `<MintlifyTemplate>` updated

### Sample app changes

- `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Models/User.cs` — add `Point HomeLocation`
- `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Migrations/` — new migration
- `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Models/RestierTestContext.SeedData.cs` — seed coordinates

### Solution

- `RESTier.slnx` — add the four new projects (two `.Spatial` source projects, two test projects)

### Not changed in spec A

- No custom `IFilterBinder`. `geo.distance` and similar operators remain non-translatable in `$filter`. Spec A documents this and asserts it via a negative test.
- No source generator or `[SpatialProperty]`-style sugar. Users wanting to expose Microsoft.Spatial types directly on entities (the inverse of the current spec) wait for spec C.
- EF6 sample app — unchanged.

## Out of scope (deferred)

| Deferred to | Item |
|-------------|------|
| Spec B | Custom `IFilterBinder` so `geo.distance` / `geo.length` / `geo.intersects` translate to `DbGeography.Distance` / `Geometry.Distance` server-side. |
| Spec C | Source-generator or convention-driven sugar for users who prefer Microsoft.Spatial-typed entity properties (inverse of spec A's storage-typed model). |
| Later | `geo.*` `$orderby` support, default SRID configuration, very-large-geometry streaming via `Edm.Stream`. |
