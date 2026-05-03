# API Versioning for RESTier — Design Spec

**Date:** 2026-05-03
**Status:** Draft
**Tracks:** [OData/RESTier#662](https://github.com/OData/RESTier/issues/662)
**Related external work:** [Asp.Versioning.OData](https://github.com/dotnet/aspnet-api-versioning)

## Goal

Make RESTier a first-class consumer of [`Asp.Versioning`](https://github.com/dotnet/aspnet-api-versioning) for **URL-segment** API versioning, so users can register multiple versions of a Restier API in the same host with versioned `$metadata`, versioned OpenAPI documents, and standard version-discovery response headers — with no changes to the existing `Microsoft.Restier.AspNetCore` request pipeline.

## Non-goals (initial release)

- **Header / query-string / media-type versioning.** RESTier's dynamic route transformer keys off the URL prefix; supporting other version readers requires a deeper rewrite of route resolution and is deferred.
- **EDM-level deprecation annotations.** Emitting `OData-Deprecation` annotations on entity sets/properties overlaps with the in-flight OpenAPI annotation work and deserves its own design.
- **Versioned `Microsoft.Restier.EntityFramework` (EF6) bindings.** The same patterns work without special glue; documented as such.
- **Auto-default-version routing** at the bare prefix (e.g., serving `/api` from V2 implicitly). Users opt in by registering a non-versioned route at the bare prefix themselves.
- **Automatic 410/Gone after sunset.** Sunset header is emitted; enforcement is a future enhancement.

## Background

### What RESTier currently does

`RestierODataOptionsExtensions.AddRestierRoute<TApi>(prefix, configureRouteServices, …)` registers an OData route at `prefix`:
- builds the EDM from `RestierWebApiModelBuilder` + `RestierWebApiModelExtender` + `RestierWebApiOperationModelBuilder` + `ConventionBasedAnnotationModelBuilder`, all driven by `TApi : ApiBase`;
- registers the per-route DI container via `ODataOptions.AddRouteComponents(prefix, model, services => …)`;
- marks the container with `RestierRouteMarker` so `MapRestier()` can identify Restier routes among all OData routes.

`MapRestier()` then iterates `ODataOptions.RouteComponents` and registers a `DynamicRouteValueTransformer` (`RestierRouteValueTransformer`) per Restier prefix. The transformer parses OData URLs at request time, fills the OData feature on `HttpContext`, and dispatches to the single generic `RestierController`. There is no user-written, attribute-decorated controller.

### Implication for versioning

The existing per-prefix model already accommodates URL-segment versioning mechanically — `NorthwindApiV1` at `api/v1`, `NorthwindApiV2` at `api/v2` works today without code changes. What's missing is integration with the `Asp.Versioning` ecosystem:
- `IApiVersionDescriptionProvider` doesn't know about RESTier routes
- `[ApiVersion]` attributes on `ApiBase` types are ignored
- NSwag/Swagger documents are named by full route prefix instead of by version
- Standard `api-supported-versions`, `api-deprecated-versions`, `Sunset` headers aren't emitted

This design fills that gap with an opt-in package.

## High-level architecture

```
ASP.NET Core MVC + Asp.Versioning  (user-configured AddApiVersioning())
                                                                     │
Microsoft.Restier.AspNetCore.Versioning  (NEW, opt-in package)       │
  • AddRestierVersionedApi<TApi>(...) extensions                     │
  • Reads [ApiVersion] from ApiBase types                            │
  • Composes route prefix:  basePrefix + "/" + segmentFormatter(v)   ▼
  • Calls existing AddRestierRoute<TApi>(composedPrefix, ...)
  • Registers RestierApiVersionRegistry (singleton)
  • Registers IApiVersionDescriptionProvider adapter
  • Provides UseRestierVersionHeaders() middleware

Microsoft.Restier.AspNetCore  (UNCHANGED except for one DI marker)
  • AddRestierRoute<TApi>(prefix, ...)  — works as today
```

Two existing packages get small, registry-aware updates:

- **`Microsoft.Restier.AspNetCore.NSwag`** — when the registry is in DI, OpenAPI documents are looked up by version group name (`v1`, `v2`) instead of route prefix. Falls back to prefix-based behavior when the registry is absent (full back-compat).
- **`Microsoft.Restier.AspNetCore.Swagger`** — mirrored change.

A new sample, **`Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore`**, demonstrates end-to-end wiring with a real V1/V2 delta.

## Public API

### Registration

```csharp
// Attribute-driven (canonical path)
[ApiVersion("1.0", Deprecated = true)]
public class NorthwindApiV1 : EntityFrameworkApi<NorthwindContextV1> { }

[ApiVersion("2.0")]
public class NorthwindApiV2 : EntityFrameworkApi<NorthwindContextV2> { }

services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(2, 0);
    o.ReportApiVersions = true;
    o.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer();

// Registers RestierApiVersionRegistry + IApiVersionDescriptionProvider adapter
// in outer DI, and arms the static-during-Startup accessor used by the
// ODataOptions-level extensions below. Required when using AddRestierVersionedApi.
services.AddRestierApiVersioning();

services
    .AddControllers()
    .AddRestier(options =>
    {
        options.Select().Expand().Filter().OrderBy().SetMaxTop(100).Count();

        options
          .AddRestierVersionedApi<NorthwindApiV1>("api", restierServices =>
          {
              restierServices.AddEFCoreProviderServices<NorthwindContextV1>(...);
          })
          .AddRestierVersionedApi<NorthwindApiV2>("api", restierServices =>
          {
              restierServices.AddEFCoreProviderServices<NorthwindContextV2>(...);
          });
    });

// in Configure(...)
app.UseRouting();
app.UseRestierVersionHeaders();   // before MapRestier
app.UseEndpoints(e =>
{
    e.MapControllers();
    e.MapRestier();
});
```

### Extension surface

```csharp
namespace Microsoft.Restier.AspNetCore.Versioning;

public static class RestierVersionedODataOptionsExtensions
{
    // Attribute-driven (reads [ApiVersion] from TApi)
    public static ODataOptions AddRestierVersionedApi<TApi>(
        this ODataOptions oDataOptions,
        string basePrefix,
        Action<IServiceCollection> configureRouteServices,
        Action<RestierVersioningOptions> configureVersioning = null,
        bool useRestierBatching = true,
        RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase)
        where TApi : ApiBase;

    // Imperative — for users who don't want [ApiVersion] on their class
    public static ODataOptions AddRestierVersionedApi<TApi>(
        this ODataOptions oDataOptions,
        ApiVersion apiVersion,
        string basePrefix,
        Action<IServiceCollection> configureRouteServices,
        Action<RestierVersioningOptions> configureVersioning = null,
        bool useRestierBatching = true,
        RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase)
        where TApi : ApiBase;

    // One ApiBase serving multiple versions
    public static ODataOptions AddRestierVersionedApi<TApi>(
        this ODataOptions oDataOptions,
        IEnumerable<ApiVersion> apiVersions,
        string basePrefix,
        Action<IServiceCollection> configureRouteServices,
        Action<RestierVersioningOptions> configureVersioning = null,
        bool useRestierBatching = true,
        RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase)
        where TApi : ApiBase;
}

public sealed class RestierVersioningOptions
{
    /// <summary>How to render an ApiVersion as a URL segment. Defaults to <see cref="ApiVersionSegmentFormatters.Major"/>.</summary>
    public Func<ApiVersion, string> SegmentFormatter { get; set; } = ApiVersionSegmentFormatters.Major;

    /// <summary>Override the composed route prefix entirely (skips SegmentFormatter and basePrefix composition).</summary>
    public string ExplicitRoutePrefix { get; set; }
}

public static class ApiVersionSegmentFormatters
{
    public static Func<ApiVersion, string> Major      { get; } = v => $"v{v.MajorVersion}";
    public static Func<ApiVersion, string> MajorMinor { get; } = v => $"v{v.MajorVersion}.{v.MinorVersion}";
}

public static class RestierVersionedApplicationBuilderExtensions
{
    /// <summary>Adds a middleware that emits api-supported-versions / api-deprecated-versions / Sunset headers on Restier responses.</summary>
    public static IApplicationBuilder UseRestierVersionHeaders(this IApplicationBuilder app);
}
```

### Registry types

The **read-only contract** lives in **`Microsoft.Restier.AspNetCore`** so NSwag/Swagger consume it without picking up the `Asp.Versioning` dependency. The descriptor exposes only primitive metadata (string-formatted version), not the typed `ApiVersion` — keeping the base package free of `Asp.Versioning.Abstractions`.

```csharp
namespace Microsoft.Restier.AspNetCore.Versioning;   // namespace alias inside the AspNetCore package

public interface IRestierApiVersionRegistry
{
    IReadOnlyList<RestierApiVersionDescriptor> Descriptors { get; }
    RestierApiVersionDescriptor FindByPrefix(string routePrefix);
    RestierApiVersionDescriptor FindByGroupName(string groupName);
}

public sealed class RestierApiVersionDescriptor
{
    public string Version { get; }           // "1.0", "2.0", etc.
    public string RoutePrefix { get; }       // composed (e.g., "api/v1")
    public Type ApiType { get; }             // e.g., typeof(NorthwindApiV1)
    public bool IsDeprecated { get; }
    public string GroupName { get; }         // e.g., "v1" — used as OpenAPI doc name
    public DateTimeOffset? SunsetDate { get; }
}
```

The **concrete implementation** and a strongly-typed view live in **`Microsoft.Restier.AspNetCore.Versioning`** (which references `Asp.Versioning.OData`):

```csharp
namespace Microsoft.Restier.AspNetCore.Versioning;

internal sealed class RestierApiVersionRegistry : IRestierApiVersionRegistry
{
    public RestierApiVersionDescriptor Add(
        ApiVersion apiVersion, string routePrefix, Type apiType,
        bool deprecated, string groupName, DateTimeOffset? sunset);

    // IRestierApiVersionRegistry members

    public RestierApiVersionDescriptor FindByVersion(ApiVersion apiVersion);  // typed extension
}
```

NSwag and Swagger consume `IRestierApiVersionRegistry` (resolved via `IServiceProvider.GetService`, null-tolerant). The Versioning package registers the concrete `RestierApiVersionRegistry` against both the interface and itself.

## Internal components

### `Microsoft.Restier.AspNetCore.Versioning` (new project)

| File | Purpose |
|------|---------|
| `Extensions/RestierApiVersioningServiceCollectionExtensions.cs` | `services.AddRestierApiVersioning()` — registers the registry, the `IApiVersionDescriptionProvider` adapter, arms the Startup-time accessor. |
| `Extensions/RestierVersionedODataOptionsExtensions.cs` | The three `AddRestierVersionedApi<TApi>` overloads. |
| `Extensions/RestierVersionedApplicationBuilderExtensions.cs` | `UseRestierVersionHeaders()`. |
| `RestierApiVersionRegistry.cs` | Internal concrete `IRestierApiVersionRegistry` implementation; mutable from inside the package. |
| `RestierVersioningOptions.cs` | Per-route options. |
| `ApiVersionSegmentFormatters.cs` | Built-in formatters. |
| `Internal/ApiVersionAttributeReader.cs` | Reads `[ApiVersion]` / deprecated / sunset metadata. |
| `Internal/RestierApiVersioningStartupContext.cs` | Static-during-Startup accessor that bridges `services.AddRestierApiVersioning()` to the `ODataOptions` lambda. |
| `Internal/RestierApiVersionDescriptionProvider.cs` | `IApiVersionDescriptionProvider` adapter sourced from the registry. |
| `Middleware/RestierVersionHeadersMiddleware.cs` | Adds version-discovery headers based on the matched prefix. |

Targets: `net8.0`, `net9.0`, `net48` (matching the rest of the solution).

Dependencies: `Microsoft.Restier.AspNetCore`, `Asp.Versioning.OData`, `Asp.Versioning.Mvc.ApiExplorer`.

### `Microsoft.Restier.AspNetCore.NSwag` (small change)

`RestierOpenApiDocumentGenerator` and `RestierOpenApiMiddleware` gain an optional `RestierApiVersionRegistry` constructor parameter resolved from DI (null-tolerant). When the registry is present, the URL pattern becomes `/openapi/{groupName}/openapi.json` and lookup is `registry.FindByGroupName(documentName)` first, with fallback to the existing prefix-based path. When the registry is absent, behavior is unchanged.

### `Microsoft.Restier.AspNetCore.Swagger` (mirrored change)

Same shape, applied to the equivalent Swashbuckle integration code.

### `Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore` (new sample)

Two API classes with a real surface delta:
- `NorthwindApiV1` — `[ApiVersion("1.0", Deprecated = true)]`. Customers (no `Email`), Orders.
- `NorthwindApiV2` — `[ApiVersion("2.0")]`. Customers (with `Email`), Orders, new `OrderShipments` set, deprecates the legacy `GetTopCustomers` function.

Two `DbContext` subclasses (`NorthwindContextV1`, `NorthwindContextV2`) hide V2-only members from V1's model via `OnModelCreating` `Ignore(...)` calls — the simplest pattern that matches RESTier's "the type defines the surface" philosophy.

`Startup.cs` wires `AddApiVersioning` → `AddRestier` → `AddRestierVersionedApi<...>` → `UseRestierVersionHeaders` → `MapRestier`. NSwag UI dropdown lists `v1` and `v2`.

### `Microsoft.Restier.AspNetCore` — minimal additions

Two type-only additions, no behavior changes:

- `IRestierApiVersionRegistry` (interface)
- `RestierApiVersionDescriptor` (record-style class)

Both live in a new `Microsoft.Restier.AspNetCore.Versioning` namespace within the package. No new dependencies — they reference only `System.*`.

**Why here, not in the Versioning package?** NSwag and Swagger need to consume the registry contract without taking a dependency on `Asp.Versioning.OData`. Putting the read-only contract in the base package (which both already reference) is the simplest path. The Versioning package owns the mutable concrete implementation.

### Mechanism for populating the registry from inside the `AddRestier` lambda

`AddRestierVersionedApi` is invoked on `ODataOptions`, but the `RestierApiVersionRegistry` lives in outer DI (so middleware and `IApiVersionDescriptionProvider` can resolve it). To bridge:

1. The user calls `services.AddRestierApiVersioning()` once in `ConfigureServices`. This:
   - Registers `RestierApiVersionRegistry` as a singleton (and against `IRestierApiVersionRegistry`).
   - Registers the `IApiVersionDescriptionProvider` adapter.
   - Sets a static-during-Startup accessor — `RestierApiVersioningStartupContext.Current` — to the registry instance just created. This is a thread-safe assignment, valid for the lifetime of `ConfigureServices`. (Same pattern as `WebHostBuilderContext` and similar Startup-time accessors.)

2. Inside the `AddRestier` lambda, `AddRestierVersionedApi` reads `RestierApiVersioningStartupContext.Current` and adds descriptors to the registry. If the accessor is null (user forgot `AddRestierApiVersioning()`), it throws `InvalidOperationException` with a clear message naming the missing call.

3. After `ConfigureServices` completes, the static accessor is cleared (or simply ignored — by then the registry is in DI and consumers resolve it the normal way).

This keeps the user-facing call sites identical to today's `AddRestierRoute` pattern (no new lambda parameter), at the cost of one explicit `AddRestierApiVersioning()` registration. The throw-on-missing makes the failure mode unambiguous.

## Data flow

### Registration time

```
AddRestierVersionedApi<NorthwindApiV1>("api", svc => {...})
    │
    ├─ ApiVersionAttributeReader.Read(typeof(NorthwindApiV1))
    │   → ApiVersion(1,0), IsDeprecated=true, SunsetDate=null
    │
    ├─ Compose prefix: "api" + "/" + segmentFormatter(v) → "api/v1"
    │
    ├─ Ensure RestierApiVersionRegistry singleton exists
    │
    ├─ Append RestierApiVersionDescriptor to registry
    │
    └─ Delegate to existing oDataOptions.AddRestierRoute<NorthwindApiV1>(
         "api/v1", configureRouteServices, useRestierBatching, namingConvention)
```

### Request time — versioned OData call

```
GET /api/v1/Customers('ALFKI')
    │
    ├─ ASP.NET routing matches the dynamic catch-all registered at "api/v1"
    │   by MapRestier (UNCHANGED)
    │
    ├─ RestierRouteValueTransformer.TransformAsync (UNCHANGED)
    │   parses, dispatches to RestierController.Get
    │
    ├─ RestierVersionHeadersMiddleware:
    │   - matches request prefix against registry
    │   - sets api-supported-versions: 1.0, 2.0
    │   - sets api-deprecated-versions: 1.0
    │   - if SunsetDate set: Sunset: <RFC 1123 date>
    │
    └─ Response returned with versioning headers.
```

### Request time — versioned OpenAPI doc

```
GET /openapi/v1/openapi.json
    │
    ├─ RestierOpenApiMiddleware:
    │   - documentName = "v1"
    │   - if registry resolved: descriptor = registry.FindByGroupName("v1")
    │     routePrefix = descriptor.RoutePrefix  // "api/v1"
    │   - else: routePrefix = documentName  // legacy fallback
    │
    └─ Generate OpenAPI from the EDM at that prefix → return JSON
```

### Asp.Versioning header reporting overlap

Asp.Versioning's `ReportApiVersions = true` reports headers via MVC's response filters; RESTier requests are dispatched by the dynamic route transformer in a way Asp.Versioning's filters won't always catch. `RestierVersionHeadersMiddleware` fills that gap on Restier-prefixed responses. The two are not in conflict — the middleware checks for already-set headers and won't duplicate them. Documented in the guide.

## Error handling and edge cases

| Scenario | Behavior |
|----------|----------|
| `[ApiVersion]` missing on attribute-driven path | `InvalidOperationException` at registration with the type name and a one-line fix (the imperative overload). |
| Same `(ApiVersion, basePrefix)` registered twice | `InvalidOperationException` listing both API types. |
| Different versions colliding on the composed prefix (custom formatter collision) | `InvalidOperationException` naming both versions and the colliding prefix. |
| `[ApiVersion]` declares state conflicting with imperative override | Imperative call wins. Documented in XML doc. |
| Multiple versions on one `ApiBase` | Each becomes an independent route registration; descriptors are independent but share `ApiType`. |
| Request to `/api` (no version segment) | 404 unless the user registers a non-versioned `AddRestierRoute<TApi>` at `"api"` themselves. |
| Versioning package present, no versioned routes | Empty registry; NSwag/Swagger glue falls back; middleware no-ops. |
| Versioning package absent, NSwag present | Existing prefix-based behavior preserved. |
| Batching at `/api/v1/$batch` | Works automatically: each versioned route gets its own `RestierBatchHandler { PrefixName = "api/v1" }` from the existing `AddRestierRoute`. |
| Sunset date in the past | `Sunset` header still emitted; no automatic 410. Future enhancement. |
| `UseRestierVersionHeaders` not registered | No exception; versioning routes simply don't carry the headers. |

## Testing strategy

### Unit tests — `Microsoft.Restier.Tests.AspNetCore.Versioning` (new project)

- `ApiVersionAttributeReader` — reads major/minor; deprecated; sunset; throws when missing.
- `RestierApiVersionRegistry` — add/find by version, prefix, group; collision detection.
- `ApiVersionSegmentFormatters` — `Major`, `MajorMinor`, custom delegate produce expected segments for representative versions (`1.0`, `2.1`, `1-Beta`).
- `RestierVersionedODataOptionsExtensions` — composes correct prefix; calls underlying `AddRestierRoute`; populates registry; rejects duplicates; honors `ExplicitRoutePrefix`; imperative overload bypasses attribute reader.
- `RestierApiVersionDescriptionProvider` — surfaces registry entries with correct group names and deprecated flags.
- `RestierVersionHeadersMiddleware` — adds expected headers for matched prefix; no-ops for unmatched paths; emits `Sunset` only when set; doesn't duplicate headers already present.

### Integration tests — same project, `IntegrationTests/`

Breakdance-style in-memory host with two versioned APIs:

| Scenario | Assertion |
|----------|-----------|
| `GET /api/v1/$metadata` | EDM matches V1 surface; V2-only members absent. |
| `GET /api/v2/$metadata` | EDM matches V2 surface. |
| `GET /api/v1/Customers` | 200; uses V1 entity set. |
| `GET /api/v3/Customers` | 404 (no such version). |
| `POST /api/v1/$batch` (one inner GET Customers) | 200; routed to V1. |
| `POST /api/v2/$batch` (one inner GET Customers) | 200; routed to V2. |
| Any 200 from a versioned route | Carries `api-supported-versions` and `api-deprecated-versions` headers. |
| V1 (deprecated) response | Carries `Sunset` if a sunset date is configured. |
| `GET /openapi/v1/openapi.json` | Returns V1-shaped OpenAPI; `info.version == "1.0"`. |
| `GET /openapi/v2/openapi.json` | Returns V2-shaped OpenAPI. |
| `GET /openapi/api/v1/openapi.json` (legacy path) | Falls back to prefix lookup; returns V1 doc. |

### Existing test projects

- `Microsoft.Restier.Tests.AspNetCore` — one regression test confirming `MapRestier` still works without versioning (no behavior change on the unversioned path).
- `Microsoft.Restier.Tests.AspNetCore.NSwag` and `…Swagger` — tests for registry-aware doc-name lookup and registry-absent fallback.

### Sample-based smoke (manual, documented)

The `NorthwindVersioned` sample doubles as a runnable end-to-end check.

## Documentation

New page **`src/Microsoft.Restier.Docs/guides/server/api-versioning.mdx`**:

- Why versioning
- Quickstart (10–15 line two-version sample)
- The pattern — one `ApiBase` per version; sharing an EF model with per-version `Ignore`s
- Registration API — both overloads; segment formatters; `ExplicitRoutePrefix`
- Versioning headers — what they look like, how to enable, interaction with `Asp.Versioning`'s own reporting
- Versioned OpenAPI — NSwag and Swagger doc-name behavior; URL paths
- Versioned `$metadata` — what to expect
- Limitations — header / query / media-type versioning not supported; pointer to a tracking issue. EDM-level deprecation annotations not yet emitted.
- Migrating from unversioned — short upgrade guide

Cross-links from `nswag.mdx`, `swagger.mdx`, `index.mdx`. Nav update in `Microsoft.Restier.Docs.docsproj`'s `<MintlifyTemplate>` to put the page under "Server guides".

A short release note entry under `release-notes/`.

## Build and packaging

- New csproj `src/Microsoft.Restier.AspNetCore.Versioning/Microsoft.Restier.AspNetCore.Versioning.csproj` — multi-targets `net8.0;net9.0;net48`, signed with `restier.snk`, warnings-as-errors, implicit usings off, nullable off (matching the rest of the solution).
- New csproj `test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj` — xUnit v3, FluentAssertions (AwesomeAssertions), NSubstitute.
- New sample `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/...`.
- All four added to `RESTier.slnx`.
- Docs project `Microsoft.Restier.Docs.docsproj` includes `Microsoft.Restier.AspNetCore.Versioning` in its source list so the API reference is generated.

## Open questions for the implementation plan

- Whether to expose `IApiVersionDescriptionProvider` as a *replacement* or *additional* provider when the user's MVC controllers also use Asp.Versioning. Default: additional (do not replace), so the user's MVC-controller versions and Restier versions both surface. Tested either way.
- Whether `RestierApiVersionDescriptor` should expose the strongly-typed `ApiVersion` via an extension method on `IRestierApiVersionRegistry` (defined in the Versioning package) for consumers that are willing to take the dependency. Default: yes, as a quality-of-life affordance.
- Final naming of `RestierApiVersioningStartupContext` — bikeshed at implementation time.
