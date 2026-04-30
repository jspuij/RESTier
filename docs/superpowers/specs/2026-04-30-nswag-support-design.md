# Design: Add NSwag + ReDoc support to Restier

**Date:** 2026-04-30
**Status:** Draft (pending user review)
**Branch:** `feature/vnext`

## Goal

Add a new `Microsoft.Restier.AspNetCore.NSwag` package that integrates Restier with [NSwag](https://github.com/RicoSuter/NSwag) and [ReDoc](https://redocly.com/redoc), making NSwag the recommended OpenAPI surface for Restier. The existing `Microsoft.Restier.AspNetCore.Swagger` package continues to ship unchanged as a Swashbuckle-based alternative.

The design also introduces a "combined Restier + plain ASP.NET Core controller" scenario in the Northwind sample, demonstrated as two separate OpenAPI documents: one for the Restier route, one for plain MVC controllers.

## Context

Today Restier ships `Microsoft.Restier.AspNetCore.Swagger`, which:

- Generates OpenAPI from the EDM model via `Microsoft.OpenApi.OData`.
- Serves JSON at `/swagger/{name}/swagger.json` via custom middleware (`RestierOpenApiMiddleware`).
- Hosts Swashbuckle's Swagger UI at `/swagger`.
- Works because no MVC controllers are scanned at all — `RestierController` therefore never leaks into any document.

NSwag is a widely-used alternative with stronger tooling: code generation (NSwag.MSBuild, NSwagStudio), ReDoc and Swagger UI 3 hosts, and a richer extensibility model (`IDocumentProcessor`, `IOperationProcessor`). NSwag also discovers ASP.NET Core MVC controllers via ApiExplorer — which means combining a Restier route with plain controllers in one application is supported by NSwag in a way Swashbuckle is not (in our integration).

NSwag uses its own `NSwag.OpenApiDocument` type, separate from `Microsoft.OpenApi.OpenApiDocument`. The two are not interchangeable. The integration must bridge them.

## Scope decisions

| Decision | Choice | Reason |
|---|---|---|
| Q1: Integration depth | **Full NSwag integration**, not just UI swap | Enables NSwag's tooling and processors; matches non-Restier NSwag UX. |
| Q2: Default UI | **ReDoc** | User preference; cleaner reading experience. Swagger UI 3 also wired up via NSwag for try-it-out. |
| Q3: Existing Swagger package | **Keep, position as alternative** | Backwards-compatible; users with Swashbuckle investment unaffected. |
| Q4: Document topology | **One doc per Restier route + a separate "controllers" doc** | Cleanest separation, no duplicated controllers across docs, matches today's per-route Swagger behaviour. |
| Q5: API naming | **NSwag-branded with separate `Use*` methods** | Explicit control over which UIs are enabled. JSON path moves to `/openapi/...` to avoid Swashbuckle collision. |
| Q6: EDM → NSwag bridge | **JSON round-trip** | Robust across version drift of either library; per-request cost matches existing Swagger middleware. |
| Q7: `RestierController` filtering | **Automatic via MVC `IApplicationModelConvention`** | Sets `ApiExplorerSettings.IgnoreApi = true` globally so NSwag, Swashbuckle, and .NET 9 OpenAPI all skip it without per-document config. |
| Q8: Sample changes | **Northwind = combined scenario; Postgres = minimal NSwag** | Northwind already had Swagger and is the more fleshed-out sample; Postgres stays lean. |
| Q9: Doc nav order | **NSwag first, Swagger second** | Reflects new "recommended path" positioning. |
| Q10: TFMs | `net8.0;net9.0;net10.0` | Same as Swagger package and rest of suite. |

## Solution layout

### New project — `src/Microsoft.Restier.AspNetCore.NSwag/`

Sibling of `src/Microsoft.Restier.AspNetCore.Swagger/`, same csproj shape.

```
Microsoft.Restier.AspNetCore.NSwag.csproj         # net8.0;net9.0;net10.0
RestierOpenApiDocumentGenerator.cs                # internal — calls Microsoft.OpenApi.OData
RestierNSwagDocumentProvider.cs                   # internal — JSON-roundtrip bridge to NSwag.OpenApiDocument
RestierControllerApiExplorerConvention.cs         # internal — IApplicationModelConvention
Extensions/
  IServiceCollectionExtensions.cs                 # AddRestierNSwag
  IApplicationBuilderExtensions.cs                # UseRestierOpenApi / UseRestierReDoc / UseRestierNSwagUI
README.md
```

**Dependencies:**
- `Microsoft.OpenApi.OData` (`[3.*, 4.0.0)`) — same constraint as the Swagger package.
- `NSwag.AspNetCore` 14.x.
- ProjectReference to `Microsoft.Restier.AspNetCore`.

**Key csproj properties** (mirroring Swagger): `<TargetFrameworks>net8.0;net9.0;net10.0;</TargetFrameworks>`, `<StrongNamePublicKey>$(StrongNamePublicKey)</StrongNamePublicKey>`, `<DocumentationFile>$(DocumentationFile)\$(AssemblyName).xml</DocumentationFile>`.

### New test project — `test/Microsoft.Restier.Tests.AspNetCore.NSwag/`

Mirrors `Microsoft.Restier.Tests.AspNetCore.Swagger` structurally; broader coverage (see Testing).

### Solution wiring — `RESTier.slnx`

Source project under `/src/Web/`, test project under `/test/Web/`, alongside their Swagger counterparts.

### Docs project — `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj`

- Add `<ProjectReference>` and matching `<_DocsSourceProject>` for the new package so the SDK builds it for `net8.0` and emits API-reference MDX.
- Update `<MintlifyTemplate>` Server group page list:
  ```
  guides/server/swagger;
  ```
  becomes:
  ```
  guides/server/nswag;
  guides/server/swagger;
  ```
- `docs.json` is regenerated by the SDK on build and committed.

## Document generation pipeline

### Per-route Restier docs

1. **`RestierOpenApiDocumentGenerator` (internal, shared)** — same shape as the existing one in the Swagger project.
   - Looks up `ODataOptions.RouteComponents[routePrefix]` to get the `IEdmModel`.
   - Sets `OpenApiConvertSettings.TopExample` from `ODataValidationSettings.MaxTop` (default 5).
   - Sets `OpenApiConvertSettings.ServiceRoot` from `request.Scheme/Host/PathBase/prefix`.
   - Invokes user's `Action<OpenApiConvertSettings>` configurator (overrides defaults).
   - Returns `model.ConvertToOpenApi(settings)` — a `Microsoft.OpenApi.OpenApiDocument`.

2. **`RestierNSwagDocumentProvider` (internal)** — bridges to NSwag.
   - On each request for a registered Restier-route document name:
     1. Call `RestierOpenApiDocumentGenerator.GenerateDocument(...)`.
     2. Serialize via `SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_0)`.
     3. Deserialize via `NSwag.OpenApiDocument.FromJsonAsync(json)`.
     4. Return the NSwag document.
   - Exposed to NSwag's middleware via NSwag's document-resolution surface (`IOpenApiDocumentGenerator` or equivalent — final hook chosen during implementation by reading NSwag 14.x source). The provider is registered for every Restier route prefix (`""` → `"default"`, `"v3"` → `"v3"`), mirroring the Swagger package's naming.

3. **Per-request regeneration** — same as today's Swagger middleware, because `ServiceRoot` depends on the inbound request.

### Plain MVC controllers doc

Standard NSwag setup — **the user registers it themselves** in `Program.cs`:

```csharp
services.AddOpenApiDocument(c => c.DocumentName = "controllers");
// in pipeline:
app.UseOpenApi();
app.UseReDoc(c => { c.DocumentPath = "/swagger/controllers/swagger.json"; c.Path = "/redoc/controllers"; });
```

Our package does not register or modify this document. The user retains full control over MVC scanning settings.

### Auto-filtering `RestierController`

`AddRestierNSwag()` registers `RestierControllerApiExplorerConvention : IApplicationModelConvention` via `MvcOptions.Conventions.Add(...)`. The convention sets `ActionModel.ApiExplorer.IsVisible = false` (equivalent to `[ApiExplorerSettings(IgnoreApi = true)]`) on every action whose controller type is `RestierController` or a subclass.

This is global, ApiExplorer-level — NSwag, Swashbuckle, and .NET 9 OpenAPI all use ApiExplorer for MVC scanning, so the filter applies regardless of which generator the user picks for their plain-controllers doc. No per-document config required.

## Public API surface

### Service registration (`Microsoft.Extensions.DependencyInjection`)

```csharp
public static class Restier_AspNetCore_NSwag_IServiceCollectionExtensions
{
    public static IServiceCollection AddRestierNSwag(
        this IServiceCollection services,
        Action<OpenApiConvertSettings> openApiSettings = null);
}
```

Registers:
- `IHttpContextAccessor`.
- The `OpenApiConvertSettings` configurator (if non-null), as a singleton.
- `RestierControllerApiExplorerConvention` via `services.Configure<MvcOptions>(o => o.Conventions.Add(...))`.
- The internal NSwag document provider plus one named registration per Restier route prefix.

### Pipeline (`Microsoft.AspNetCore.Builder`)

```csharp
public static class Restier_AspNetCore_NSwag_IApplicationBuilderExtensions
{
    public static IApplicationBuilder UseRestierOpenApi(this IApplicationBuilder app);   // /openapi/{name}/openapi.json
    public static IApplicationBuilder UseRestierReDoc(this IApplicationBuilder app);     // /redoc/{name}
    public static IApplicationBuilder UseRestierNSwagUI(this IApplicationBuilder app);   // /swagger (NSwag UI 3)
}
```

Each method is independent — users call any combination.

`UseRestierOpenApi` and `UseRestierReDoc` enumerate `ODataOptions.GetRestierRoutePrefixes()` and register one middleware/path per route. `UseRestierNSwagUI` registers a single Swagger UI 3 host with a dropdown listing every Restier route document.

### URL contract

| Path | Source | Content |
|---|---|---|
| `/openapi/default/openapi.json` | Restier route `""` | EDM-derived OpenAPI 3.0 |
| `/openapi/{prefix}/openapi.json` | Restier route `{prefix}` | EDM-derived OpenAPI 3.0 |
| `/redoc/default` | Restier route `""` | ReDoc page |
| `/redoc/{prefix}` | Restier route `{prefix}` | ReDoc page |
| `/swagger` | NSwag UI 3 | Dropdown of all Restier docs |
| `/swagger/controllers/swagger.json` | User's `AddOpenApiDocument("controllers")` | Plain MVC controllers (NSwag default path) |
| `/redoc/controllers` | User's `UseReDoc(...)` | ReDoc for the controllers doc |

### Path-collision handling

Restier-NSwag JSON lives under `/openapi/...`; Swashbuckle's existing path is `/swagger/{name}/swagger.json`. Referencing both `Microsoft.Restier.AspNetCore.Swagger` and `Microsoft.Restier.AspNetCore.NSwag` in the same app is **not supported** — the docs page will state this explicitly. The `/swagger` UI path will collide if both are referenced; users pick one.

### Configuration scope (intentionally narrow)

URL paths are not configurable in v1. Users who need different paths fall back to NSwag's lower-level APIs and re-register documents themselves. Keeps the package surface small.

## Sample changes

### `Microsoft.Restier.Samples.Northwind.AspNetCore` — combined Restier + MVC

- Replace package reference: `Microsoft.Restier.AspNetCore.Swagger` → `Microsoft.Restier.AspNetCore.NSwag`.
- `Startup.cs`:
  - `services.AddRestierSwagger()` → `services.AddRestierNSwag()`.
  - Add `services.AddOpenApiDocument(c => c.DocumentName = "controllers")`.
  - Replace `app.UseRestierSwaggerUI()` with `app.UseRestierOpenApi()` + `app.UseRestierReDoc()` + `app.UseRestierNSwagUI()`.
  - Add `app.UseOpenApi()` and `app.UseReDoc(...)` for the controllers doc.
- New `Controllers/HealthController.cs` — `[ApiController]` with `GET /health/live` and `GET /health/version`. Trivial; no DB access.
- Manual browser verification (per CLAUDE.md UI-changes gate):
  - `/redoc/default` shows Northwind OData entity sets, no `HealthController` entries.
  - `/redoc/controllers` shows only `HealthController`.
  - `/swagger` shows NSwag UI 3 with the Restier route(s) in the dropdown.

### `Microsoft.Restier.Samples.Postgres.AspNetCore` — minimal NSwag

- Add package reference: `Microsoft.Restier.AspNetCore.NSwag`.
- `Program.cs`: add `services.AddRestierNSwag()` after `AddRestier(...)`; add `app.UseRestierOpenApi()` and `app.UseRestierReDoc()` after `UseEndpoints`. Skip `UseRestierNSwagUI()` to keep the minimal sample minimal.
- No MVC controllers added.
- Manual browser verification: `/redoc/v3` shows Postgres OData API.

## Documentation changes

### New page — `src/Microsoft.Restier.Docs/guides/server/nswag.mdx`

Modeled on `swagger.mdx`. Sections:

- **Intro** — recommended OpenAPI path; cross-link to Swagger page as the alternative.
- **Setup** — install + four lines (`AddRestierNSwag` + the three `Use*`); complete `Program.cs` example.
- **Usage / endpoints table** — JSON, ReDoc, Swagger UI 3 paths.
- **Configuration** — `Action<OpenApiConvertSettings>` (same `OpenApiConvertSettings` from `swagger.mdx`).
- **Multiple Restier APIs** — analogous to swagger.mdx's section, framed around NSwag named documents.
- **Combining with plain MVC controllers** — unique to NSwag. Walkthrough of `AddOpenApiDocument("controllers")` + `UseOpenApi()` + `UseReDoc(...)`. Notes the auto-filter on `RestierController`. Cross-references the Northwind sample.
- **What `AddRestierNSwag()` does for you** — `<Note>` listing the MVC convention, JSON-roundtrip bridge, per-route document registration.
- **Picking between NSwag and Swagger** — short closing section.

### Updated page — `src/Microsoft.Restier.Docs/guides/server/swagger.mdx`

- Title unchanged ("OpenAPI / Swagger Support").
- Description and lead paragraph reframe as "Swashbuckle-based alternative to NSwag."
- New `<Note>` at top: "For new projects we recommend [the NSwag integration](nswag) — it supports ReDoc, NSwagStudio, and combining Restier routes with plain ASP.NET Core controllers in the same OpenAPI document. Both packages remain supported."
- Existing Contributors table, link references, attribution **preserved exactly** (per the credits-keeping convention).

### Package README — `src/Microsoft.Restier.AspNetCore.NSwag/README.md`

Modeled on the Swagger package README. Same Contributors / link-refs sections. NSwag-specific install / wire-up steps.

### API reference

Auto-generated by the DotNetDocs SDK. No hand-editing under `api-reference/`.

### Release notes

A new entry in `release-notes/` for the version that ships this — handled at release time, not in this design.

## Testing strategy

Test project: `test/Microsoft.Restier.Tests.AspNetCore.NSwag/` (xUnit v3, FluentAssertions / AwesomeAssertions, NSubstitute per project conventions).

### Unit tests

**`Extensions/IServiceCollectionExtensionsTests.cs`** (mirrors Swagger version, expanded):
- `AddRestierNSwag_NoSettingsAction` — services registered as expected.
- `AddRestierNSwag_SettingsAction` — settings configurator captured.
- `AddRestierNSwag_RegistersApiExplorerConvention` — convention added to `MvcOptions.Conventions`.

**`Extensions/IApplicationBuilderExtensionsTests.cs`** (mirrors Swagger version, expanded from empty):
- `UseRestierOpenApi_RegistersMiddleware`.
- `UseRestierReDoc_RegistersMiddleware`.
- `UseRestierNSwagUI_RegistersMiddleware`.
- Each verifies the middleware registers without throwing on a minimal pipeline.

**`RestierOpenApiDocumentGeneratorTests.cs`** (new):
- Generates from a small EDM model via `Microsoft.Restier.Breakdance`.
- Route prefix → document name mapping (`""` → `"default"`, `"v3"` → `"v3"`).
- `TopExample` defaults to `ODataValidationSettings.MaxTop`, overridable.
- `ServiceRoot` built from request scheme/host/pathBase/prefix.

**`RestierNSwagDocumentProviderTests.cs`** (new):
- JSON round-trip lossless for `Paths` and `Components.Schemas` on a known EDM.
- Document exposed under route-prefix name to NSwag's resolver.
- Resulting `NSwag.OpenApiDocument` non-null and well-formed for a minimal EDM.

**`ApiExplorerConventionTests.cs`** (new):
- `ApplicationModel` with `RestierController` plus a sibling plain controller.
- After convention runs: `IsVisible = false` only on `RestierController` actions.

### Integration tests — `IntegrationTests/EndToEndTests.cs`

`TestServer` with a Breakdance-hosted Restier API plus one plain MVC controller:

- `GET /openapi/default/openapi.json` → 200, valid OpenAPI 3.0 JSON, contains Restier entity-set paths, does **not** contain plain controller path.
- `GET /swagger/controllers/swagger.json` (user-registered NSwag plain doc) → 200, contains plain controller path, does **not** contain `RestierController` actions (proves auto-filter works end to end).
- `GET /redoc/default` → 200, HTML referencing the OpenAPI JSON URL.
- `GET /swagger` → 200, NSwag UI 3 HTML listing the Restier docs.

### Out of scope

- **Snapshot tests of generated OpenAPI JSON.** Too brittle — drifts with `Microsoft.OpenApi.OData` and NSwag versions. Asserting structural invariants instead.
- **NSwag UI rendering tests.** NSwag's responsibility.
- **Backfilling test coverage on `Microsoft.Restier.Tests.AspNetCore.Swagger`.** That project has only two registration tests today and does not cover the document generator or middleware. Out of scope for this design — touching it expands the work beyond "add NSwag." Future task.

### `Microsoft.Restier.Tests.AspNetCore.NSwag.csproj`

- TFMs `net8.0;net9.0;net10.0`.
- ProjectReference to `Microsoft.Restier.AspNetCore.NSwag`, `Microsoft.Restier.Breakdance`, `Microsoft.Restier.Tests.Shared`.

## Verification gates

Per the project's CLAUDE.md:

- **Build:** `dotnet build RESTier.slnx` succeeds (warnings-as-errors enabled).
- **Tests:** `dotnet test RESTier.slnx` passes, including the new NSwag test project on all three TFMs.
- **Docs build:** `dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj` succeeds; `docs.json` regenerates and is committed alongside nav changes.
- **UI verification (manual):** Northwind and Postgres samples each launched in the browser; the URLs listed in the sample sections render the expected content. Required because these are UI-affecting changes.

## Risks & open questions for implementation

- **NSwag document-resolution hook.** The exact NSwag 14.x extension point for "register a pre-built document under a name" is to be confirmed during implementation by reading NSwag source. Fallback: implement via NSwag's standard `AddOpenApiDocument` with a custom `IDocumentProcessor` that swaps in our pre-built JSON-derived document. Either way the public API surface is unchanged.
- **NSwag default UI path.** NSwag's Swagger UI 3 defaults to `/swagger` and serves a dropdown across all named documents. If the user registers a `controllers` doc with `AddOpenApiDocument`, that doc will also appear in `/swagger`'s dropdown — which is desirable. Confirmed during implementation.
- **`net10.0` NSwag support.** NSwag 14.x's TFM coverage for `net10.0` to be verified before merging; if a gap exists, document the constraint and target only `net8.0;net9.0` for the v1 ship (Swagger package and rest of suite stay on `net8.0;net9.0;net10.0`).

## Out of scope

- Removing the `Microsoft.Restier.AspNetCore.Swagger` package.
- Backfilling the existing Swagger test project's coverage.
- Configuring URL paths for the NSwag JSON / ReDoc / UI endpoints.
- Code-generation tooling (NSwagStudio profile templates, NSwag.MSBuild integration). Users can configure those themselves once the OpenAPI document is served.
- Release notes content for the shipping version.
