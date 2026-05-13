# EF6 Spatial Tests — dotMorten Shim Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Unskip the EF6 spatial round-trip tests in `Microsoft.Restier.Tests.EntityFramework.Spatial` by adding the cross-platform managed `dotMorten.Microsoft.SqlServer.Types` shim as a test-only dependency, and document the EF6-on-.NET-5+ situation for users.

**Architecture:** RESTier's source projects all target `net8.0;net9.0;net10.0` only. EF6's spatial bridge (`DbGeography.FromText`, `DbSpatialServices.Default.AsText…`) reflects into the `Microsoft.SqlServer.Types` assembly at runtime, which on .NET 5+ is not present by default — not even on Windows. `dotMorten.Microsoft.SqlServer.Types` is a fully managed reimplementation distributed under the same assembly name + strong name, so EF6 picks it up transparently with zero source changes. We add the package to the spatial test project (and only there — downstream consumers stay free to pick their own backing assembly) and remove the `SqlServerTypesAvailable` probe + `Skip`/`SkipUnless` markers.

**Known dotMorten gap:** dotMorten implements WKT/WKB serialization but **omits the geometric operations** that require real computational-geometry algorithms — `STEquals` (which underpins `DbGeography.SpatialEquals` / `DbGeometry.SpatialEquals`), `STIntersects`, `STDistance`, etc. Three of the round-trip tests currently assert via `roundTrip.SpatialEquals(original)`. Under dotMorten those calls throw `NotImplementedException`, so we rewrite those three assertions to compare on the round-trip evidence we actually care about: same WKT, same SRID, same coordinate ordinates (Z/M as relevant). This is a stronger, more focused assertion anyway — `SpatialEquals` is true-by-tolerance, while WKT equality is byte-exact.

The defensive try/catch in `LibraryTestInitializer.cs` stays in place — it correctly degrades for consumers who don't install dotMorten.

**Tech Stack:** C# (.NET 8/9/10), Entity Framework 6.5.x, `dotMorten.Microsoft.SqlServer.Types` 2.x, Microsoft.Spatial, xUnit v3, AwesomeAssertions (imported as `FluentAssertions`), Mintlify MDX docs.

**Spec / context:** No formal spec — direct user request after observing that EF6 spatial tests skip on Windows .NET 8/9/10 because `Microsoft.SqlServer.Types` is .NET-Framework-only by default. See conversation memory: skip messages claim "Windows / SQL Server only" but the real requirement is the assembly, which dotMorten provides cross-platform.

---

## Conventions

- **Targets:** net8.0, net9.0, net10.0 (no net48 — solution-wide convention).
- **Brace style:** Allman. `var` preferred. Curly braces even for single-line blocks.
- **Warnings as errors:** enabled globally — code must be warning-clean.
- **Implicit usings disabled:** every `using` directive must be explicit.
- **Test framework:** xUnit v3 (`[Fact]`, `[Theory]`), AwesomeAssertions (`Should()`), NSubstitute (`Substitute.For<T>()`).
- **Package scope:** dotMorten is a **test-only** dependency. Do **not** add it to `src/Microsoft.Restier.EntityFramework.Spatial.csproj` — consumers must opt in themselves so they can also choose the official `Microsoft.SqlServer.Types` package if they prefer.
- **Commits:** small and focused; one per task. Co-author lines as the existing repo uses.

---

## File Inventory

| File | Action | Purpose |
|------|--------|---------|
| `test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj` | Modify | Add `<PackageReference>` to `dotMorten.Microsoft.SqlServer.Types`. |
| `test/Microsoft.Restier.Tests.EntityFramework.Spatial/DbSpatialConverterTests.cs` | Modify | Remove `SqlServerTypesAvailable` probe property and every `Skip` / `SkipUnless` argument. Rewrite the three `SpatialEquals`-based assertions (`Round_trip_preserves_value`, `Round_trips_LineString`, `Round_trips_Polygon`) to compare `AsText()` + `CoordinateSystemId` instead — dotMorten omits `STEquals`. All other test bodies stay untouched. |
| `test/Microsoft.Restier.Tests.EntityFramework.Spatial/EFChangeSetInitializerSpatialTests.cs` | Modify | Remove `SqlServerTypesAvailable` probe property and the `Skip` / `SkipUnless` argument. Test bodies untouched (no `SpatialEquals` use). |
| `src/Microsoft.Restier.Docs/guides/extending-restier/spatial-types.mdx` | Modify | Add a "Running EF6 spatial on .NET 5+" section explaining the assembly requirement and the dotMorten shim option (plus the official `Microsoft.SqlServer.Types` package as the Windows-only alternative). |

Files **not** touched (deliberate):
- `src/Microsoft.Restier.EntityFramework.Spatial/Microsoft.Restier.EntityFramework.Spatial.csproj` — library stays dependency-free; consumers choose.
- `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryTestInitializer.cs:208-230` — the try/catch around the EF6 SpatialPlace seed is correct defensive code for consumers/CI runs without dotMorten.

---

## Phase 1 — Wire up dotMorten and unskip the EF6 spatial unit tests

### Task 1: Add dotMorten package reference

**Files:**
- Modify: `test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj`

- [ ] **Step 1: Pick the latest stable 2.x version of dotMorten**

Run:

```bash
dotnet package search dotMorten.Microsoft.SqlServer.Types --exact-match --take 1
```

Expected: a single result showing the package with its latest stable version (e.g. `2.4.0`). Note the version — substitute it everywhere below as `<DOTMORTEN_VERSION>`.

If `dotnet package search` is unavailable, fall back to: `nuget search dotMorten.Microsoft.SqlServer.Types` or check https://www.nuget.org/packages/dotMorten.Microsoft.SqlServer.Types.

- [ ] **Step 2: Add the package reference**

Open `test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj` and add a new `<ItemGroup>` containing the package, after the existing `<ItemGroup>` with `<ProjectReference>` items:

```xml
<ItemGroup>
    <!--
        Test-only shim that ships a managed re-implementation of Microsoft.SqlServer.Types under the
        same assembly name and public key. EF6's DbSpatialServices reflects into this assembly at
        runtime; without it, DbGeography.FromText / DbSpatialServices.Default.AsText* throw because
        Microsoft's official Microsoft.SqlServer.Types ships native binaries that are .NET-Framework-
        only by default on .NET 5+.  Adding this package lets every EF6 spatial unit test in this
        project run cross-platform with zero source changes to the library under test.
    -->
    <PackageReference Include="dotMorten.Microsoft.SqlServer.Types" Version="<DOTMORTEN_VERSION>" />
</ItemGroup>
```

- [ ] **Step 3: Restore and build**

Run:

```bash
dotnet build test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj
```

Expected: build succeeds. No warnings beyond pre-existing ones.

- [ ] **Step 4: Smoke-check the probe property now passes**

The existing probe property (`SqlServerTypesAvailable`) is a perfect single-command test for "does EF6's spatial reflection see a usable assembly now?". Use it as the smoke test before removing it in Task 2.

Run a single one of the currently-skipped tests with the probe still in place — xUnit v3 will report it as **passed** rather than **skipped** if the probe succeeds:

```bash
dotnet test test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj --filter "FullyQualifiedName~ToEdm_returns_GeographyPoint_for_DbGeography_Point" --logger "console;verbosity=normal"
```

Expected: 1 test passed (no skips). If the test is still skipped, the package didn't land — check the project's output directory `bin/Debug/net8.0/` for `Microsoft.SqlServer.Types.dll`.

**Do not commit yet** — combine with Task 2 into one commit per the conventions.

---

### Task 2: Remove the probe and Skip markers in `DbSpatialConverterTests.cs`

**Files:**
- Modify: `test/Microsoft.Restier.Tests.EntityFramework.Spatial/DbSpatialConverterTests.cs`

- [ ] **Step 1: Delete the `SqlServerTypesAvailable` property**

Remove lines 14-37 entirely (the whole `/// <summary>…</summary>` block plus the property body). The file currently reads:

```csharp
    public class DbSpatialConverterTests
    {
        /// <summary>
        /// Returns true when the native <c>Microsoft.SqlServer.Types</c> assembly can be loaded
        /// by EF6's spatial loader.  On non-Windows hosts (or machines without SQL Server native
        /// types installed) the three geometry-exercising tests are skipped rather than failing.
        /// </summary>
        public static bool SqlServerTypesAvailable
        {
            get
            {
                try
                {
                    // Force EF6 to probe for the native types assembly now.
                    _ = DbSpatialServices.Default;
                    DbGeography.FromText("POINT(0 0)", 4326);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        private readonly DbSpatialConverter _converter = new();
```

After the edit it should be:

```csharp
    public class DbSpatialConverterTests
    {
        private readonly DbSpatialConverter _converter = new();
```

- [ ] **Step 2: Remove every `Skip = "…"` / `SkipUnless = nameof(SqlServerTypesAvailable)` argument**

There are eight call sites in this file (seven `[Fact(…)]` and one `[Theory(…)]`). For each, reduce the attribute to its bare form. Concretely:

Find each occurrence of:

```csharp
        [Fact(Skip = "Requires Microsoft.SqlServer.Types native assembly (Windows / SQL Server only).",
              SkipUnless = nameof(SqlServerTypesAvailable))]
```

and replace with:

```csharp
        [Fact]
```

And for the single `[Theory(…)]` occurrence:

```csharp
        [Theory(Skip = "Requires Microsoft.SqlServer.Types native assembly (Windows / SQL Server only).",
                SkipUnless = nameof(SqlServerTypesAvailable))]
```

replace with:

```csharp
        [Theory]
```

- [ ] **Step 3: Rewrite the three `SpatialEquals`-based round-trip assertions**

dotMorten omits the computational-geometry methods (`STEquals` and friends). `DbGeography.SpatialEquals` / `DbGeometry.SpatialEquals` route through `STEquals`, so under dotMorten they throw `NotImplementedException` rather than returning `true`/`false`. Three tests in this file rely on `SpatialEquals`; rewrite each to compare on the round-trip evidence that actually matters: WKT body, SRID, and (where relevant) Z ordinate. WKT equality is stronger than `SpatialEquals` for a round-trip test anyway — `SpatialEquals` is tolerance-based, WKT byte-equality is exact.

**Test 1 — `Round_trip_preserves_value` (currently at lines 77-86):**

Replace the body:

```csharp
        public void Round_trip_preserves_value()
        {
            var original = DbGeography.FromText("POINT(4.9041 52.3676)", 4326);

            var edm = _converter.ToEdm(original, typeof(GeographyPoint));
            var roundTrip = (DbGeography)_converter.ToStorage(typeof(DbGeography), edm);

            roundTrip.SpatialEquals(original).Should().BeTrue();
            roundTrip.CoordinateSystemId.Should().Be(original.CoordinateSystemId);
        }
```

with:

```csharp
        public void Round_trip_preserves_value()
        {
            var original = DbGeography.FromText("POINT(4.9041 52.3676)", 4326);

            var edm = _converter.ToEdm(original, typeof(GeographyPoint));
            var roundTrip = (DbGeography)_converter.ToStorage(typeof(DbGeography), edm);

            roundTrip.AsText().Should().Be(original.AsText());
            roundTrip.CoordinateSystemId.Should().Be(original.CoordinateSystemId);
        }
```

**Test 2 — `Round_trips_LineString` (currently at lines 90-98):**

Replace the body:

```csharp
        public void Round_trips_LineString()
        {
            var original = DbGeography.FromText("LINESTRING(0 0, 1 1, 2 2)", 4326);

            var edm = (GeographyLineString)_converter.ToEdm(original, typeof(GeographyLineString));
            var roundTrip = (DbGeography)_converter.ToStorage(typeof(DbGeography), edm);

            roundTrip.SpatialEquals(original).Should().BeTrue();
        }
```

with:

```csharp
        public void Round_trips_LineString()
        {
            var original = DbGeography.FromText("LINESTRING(0 0, 1 1, 2 2)", 4326);

            var edm = (GeographyLineString)_converter.ToEdm(original, typeof(GeographyLineString));
            var roundTrip = (DbGeography)_converter.ToStorage(typeof(DbGeography), edm);

            roundTrip.AsText().Should().Be(original.AsText());
            roundTrip.CoordinateSystemId.Should().Be(original.CoordinateSystemId);
        }
```

**Test 3 — `Round_trips_Polygon` (currently at lines 102-110):**

Replace the body:

```csharp
        public void Round_trips_Polygon()
        {
            var original = DbGeography.FromText("POLYGON((0 0, 0 1, 1 1, 1 0, 0 0))", 4326);

            var edm = (GeographyPolygon)_converter.ToEdm(original, typeof(GeographyPolygon));
            var roundTrip = (DbGeography)_converter.ToStorage(typeof(DbGeography), edm);

            roundTrip.SpatialEquals(original).Should().BeTrue();
        }
```

with:

```csharp
        public void Round_trips_Polygon()
        {
            var original = DbGeography.FromText("POLYGON((0 0, 0 1, 1 1, 1 0, 0 0))", 4326);

            var edm = (GeographyPolygon)_converter.ToEdm(original, typeof(GeographyPolygon));
            var roundTrip = (DbGeography)_converter.ToStorage(typeof(DbGeography), edm);

            roundTrip.AsText().Should().Be(original.AsText());
            roundTrip.CoordinateSystemId.Should().Be(original.CoordinateSystemId);
        }
```

Note: `DbGeography.AsText()` is the public WKT-without-SRID-prefix accessor and is implemented by dotMorten (it routes through `STAsText`, which dotMorten provides because WKT serialization is the package's whole point). The other test that uses our internal `DbSpatialServices.Default.AsTextIncludingElevationAndMeasure` path already proves dotMorten handles the WKT extraction code path.

- [ ] **Step 4: Remove the now-unused `System` using if it has no other consumer**

The probe property's `catch (Exception)` was the only user of `using System;` in this file. After removing the property, check whether anything else needs `System`. The file body uses no other `System.*` types directly (constructions like `new()` and string literals don't require it), so:

Open `test/Microsoft.Restier.Tests.EntityFramework.Spatial/DbSpatialConverterTests.cs` and remove the line:

```csharp
using System;
```

If your editor/IDE complains that `System` is still needed after the removal (unlikely), put it back — build is the source of truth.

- [ ] **Step 5: Build**

Run:

```bash
dotnet build test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj
```

Expected: build succeeds, warnings-as-errors clean.

- [ ] **Step 6: Run the file's tests**

Run:

```bash
dotnet test test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj --filter "FullyQualifiedName~DbSpatialConverterTests" --logger "console;verbosity=normal"
```

Expected: all tests pass, zero skipped. Concretely (one row per `[Fact]` / `[Theory]` data row):

- `CanConvert_returns_true_for_DbGeography` ✓
- `ToEdm_returns_GeographyPoint_for_DbGeography_Point` ✓
- `ToStorage_returns_DbGeography_for_GeographyPoint` ✓
- `Round_trip_preserves_value` ✓
- `Round_trips_LineString` ✓
- `Round_trips_Polygon` ✓
- `Preserves_Geography_SRID(srid: 4326)` ✓
- `Preserves_Geography_SRID(srid: 4269)` ✓
- `Preserves_Z_coordinate` ✓
- `Round_trips_DbGeometry_Point_with_planar_SRID` ✓
- `Null_storage_value_returns_null` ✓
- `ToStorage_with_unsupported_storage_type_throws` ✓
- `ToEdm_with_unsupported_storage_value_throws` ✓

If any test fails: most likely failure modes are (a) `AsText()` throwing `NotImplementedException` — means the test would have to use `DbSpatialServices.Default.AsTextIncludingElevationAndMeasure(roundTrip)` instead, the same path the converter itself uses, which dotMorten definitely supports because the other passing converter tests exercise it; or (b) WKT formatting normalization differences between `original` and `roundTrip` (e.g. trailing whitespace, casing of `POINT`/`LINESTRING`). If (b), investigate by printing both `AsText()` results — both go through the same dotMorten code path so they should be byte-identical, but if not, normalize via `.Replace(" ", "").ToUpperInvariant()` and document the quirk in a code comment.

---

### Task 3: Remove the probe and Skip marker in `EFChangeSetInitializerSpatialTests.cs`

**Files:**
- Modify: `test/Microsoft.Restier.Tests.EntityFramework.Spatial/EFChangeSetInitializerSpatialTests.cs`

- [ ] **Step 1: Delete the `SqlServerTypesAvailable` property**

Remove lines 16-31 (the property). Before:

```csharp
    public class EFChangeSetInitializerSpatialTests
    {
        public static bool SqlServerTypesAvailable
        {
            get
            {
                try
                {
                    _ = DbSpatialServices.Default;
                    DbGeography.FromText("POINT(0 0)", 4326);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        [Fact(Skip = "Requires Microsoft.SqlServer.Types native assembly (Windows / SQL Server only).",
              SkipUnless = nameof(SqlServerTypesAvailable))]
        public void ConvertToEfValue_dispatches_to_registered_spatial_converter_for_DbGeography()
```

After:

```csharp
    public class EFChangeSetInitializerSpatialTests
    {
        [Fact]
        public void ConvertToEfValue_dispatches_to_registered_spatial_converter_for_DbGeography()
```

- [ ] **Step 2: Build**

Run:

```bash
dotnet build test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj
```

Expected: build succeeds, warnings-as-errors clean.

- [ ] **Step 3: Run the file's tests**

Run:

```bash
dotnet test test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj --filter "FullyQualifiedName~EFChangeSetInitializerSpatialTests" --logger "console;verbosity=normal"
```

Expected: both tests pass, zero skipped:

- `ConvertToEfValue_dispatches_to_registered_spatial_converter_for_DbGeography` ✓
- `ConvertToEfValue_passes_through_when_no_converter_registered` ✓

---

### Task 4: Run the full spatial test project & commit

- [ ] **Step 1: Full test run for the project**

Run:

```bash
dotnet test test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj --logger "console;verbosity=normal"
```

Expected: every test in the project passes, zero skipped. Tally previously-skipped tests now running: 10 test rows (7 `[Fact]` + 1 `[Theory]` with 2 data rows in `DbSpatialConverterTests`, plus 1 `[Fact]` in `EFChangeSetInitializerSpatialTests`).

- [ ] **Step 2: Sanity-check the rest of the solution still builds**

The dotMorten package is contained to this one test csproj, but the `Microsoft.SqlServer.Types` assembly may now resolve in any test runner that references this project transitively. We don't expect it to, but check:

```bash
dotnet build RESTier.slnx
```

Expected: full solution builds.

- [ ] **Step 3: Stage and commit**

```bash
git add test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj \
        test/Microsoft.Restier.Tests.EntityFramework.Spatial/DbSpatialConverterTests.cs \
        test/Microsoft.Restier.Tests.EntityFramework.Spatial/EFChangeSetInitializerSpatialTests.cs

git commit -m "$(cat <<'EOF'
test(ef6-spatial): unskip round-trip tests via dotMorten managed shim

EF6's DbSpatialServices reflects into Microsoft.SqlServer.Types at runtime.
On .NET 5+ that assembly is .NET-Framework-only by default, so every spatial
round-trip test was [Skip]-marked with a misleading "Windows / SQL Server only"
reason — they skipped even on Windows .NET 8/9/10.

Add dotMorten.Microsoft.SqlServer.Types as a test-only PackageReference. It's a
fully managed reimplementation shipped under the same assembly name + public
key, so EF6 picks it up transparently with zero source changes. The library
under test (Microsoft.Restier.EntityFramework.Spatial) stays free of the
dependency — consumers choose their own backing assembly.

Removed the SqlServerTypesAvailable probe and every Skip / SkipUnless marker.
All 10 previously-skipped round-trip test rows now run and pass. The three
tests that asserted via DbGeography.SpatialEquals are rewritten to compare
WKT and SRID directly — SpatialEquals routes through STEquals, which
dotMorten omits (it's a WKT/WKB shim, not a computational-geometry engine).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 2 — Document the EF6-on-.NET-5+ situation

### Task 5: Add a "Running EF6 spatial on .NET 5+" section to `spatial-types.mdx`

**Files:**
- Modify: `src/Microsoft.Restier.Docs/guides/extending-restier/spatial-types.mdx`

- [ ] **Step 1: Insert the new section**

Open `src/Microsoft.Restier.Docs/guides/extending-restier/spatial-types.mdx`. The current end of the file (line 80-88) reads:

```mdx
## What's not yet supported

- Server-side `geo.distance` / `geo.length` / `geo.intersects` translation. Use `$filter` with these operators returns an error today; a future spec will deliver translation.
- Non-EPSG `CoordinateSystem` values throw `InvalidOperationException` on write. Default-SRID configuration is planned for a future spec.

## How it works

Round-trip flows through Microsoft.Spatial's `WellKnownTextSqlFormatter` (SQL Server extended WKT with `SRID=N;` prefix) and the storage-library WKT APIs (`DbGeography.FromText` / NTS `WKTReader`). SRID and Z/M ordinates survive both directions.
```

Insert a new `## Running EF6 spatial on .NET 5+` section **before** `## What's not yet supported`. Use the existing Mintlify components (`<Warning>`, `<Info>`, `<Tabs>`, `<CodeGroup>`) consistently with the rest of the file:

```mdx
## Running EF6 spatial on .NET 5+

EF6's spatial bridge (`DbGeography.FromText`, `DbSpatialServices.Default.AsText…`) reflects into the `Microsoft.SqlServer.Types` assembly at runtime. On **.NET Framework + Windows + SQL Server installed** that assembly lives in the GAC alongside its native `SqlServerSpatial*.dll` — no extra setup. On **.NET 5+ (including .NET 8/9/10 on Windows)** the assembly is **not present by default**, even on Windows, and any attempt to read or write a `DbGeography` / `DbGeometry` throws.

<Warning>
This affects RESTier too — if you've installed `Microsoft.Restier.EntityFramework.Spatial` on a .NET 5+ host without one of the workarounds below, `DbSpatialConverter` will throw on every read/write. `Microsoft.Restier.EntityFramework.Spatial` does **not** take a hard dependency on any specific backing assembly so that you can pick the option that fits your deployment.
</Warning>

You have two practical options.

### Option 1 — dotMorten shim (cross-platform, managed-only)

[`dotMorten.Microsoft.SqlServer.Types`](https://www.nuget.org/packages/dotMorten.Microsoft.SqlServer.Types) is a community-maintained, fully managed reimplementation distributed under the same assembly name and public key. EF6 picks it up transparently — no native libraries, runs on Windows, Linux, and macOS.

```bash
dotnet add package dotMorten.Microsoft.SqlServer.Types
```

This is what the RESTier test suite uses to exercise the EF6 spatial round-trip tests on .NET 8/9/10.

### Option 2 — Official `Microsoft.SqlServer.Types` package (Windows-only, native)

The official Microsoft package targets `netstandard2.0` / `netstandard2.1` and ships Windows-only native binaries (`SqlServerSpatial*.dll`). You must call the loader once at process start-up before any spatial operation:

```csharp
// Program.cs (or any one-time init)
Microsoft.SqlServer.Types.SqlServerTypes.Utilities
    .LoadNativeAssemblies(AppContext.BaseDirectory);
```

```bash
dotnet add package Microsoft.SqlServer.Types
```

Choose this when you need byte-for-byte parity with SQL Server's native CLR types — e.g. computations like `STDistance` or `STEquals` that must match what the database produces, or features dotMorten doesn't ship (it deliberately omits computational-geometry operations). Linux / macOS hosts will still fail on spatial operations.

<Info>
EF Core users do not need either of these — the EF Core spatial path uses NetTopologySuite, which is a self-contained managed library with no native dependencies.
</Info>
```

- [ ] **Step 2: Rebuild the docs project**

Run:

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
```

Expected: build succeeds. The DotNetDocs SDK regenerates `docs.json`; nothing else under `guides/` should change.

- [ ] **Step 3: Verify the page renders sensibly**

This is an MDX file — there's no local Mintlify renderer wired into the repo. Eyeball-check:

```bash
sed -n '78,150p' src/Microsoft.Restier.Docs/guides/extending-restier/spatial-types.mdx
```

Expected: the new section appears between the EF Core declaration tabs and `## What's not yet supported`, with all Mintlify component tags (`<Warning>`, `<Info>`) opened and closed correctly. No stray markdown.

- [ ] **Step 4: Stage and commit**

```bash
git add src/Microsoft.Restier.Docs/guides/extending-restier/spatial-types.mdx \
        src/Microsoft.Restier.Docs/docs.json

git commit -m "$(cat <<'EOF'
docs(spatial): explain EF6 + .NET 5+ Microsoft.SqlServer.Types requirement

Add a "Running EF6 spatial on .NET 5+" section to the spatial-types guide
documenting that EF6's DbSpatialServices reflects into Microsoft.SqlServer.Types
at runtime, and that this assembly is .NET-Framework-only by default — so
RESTier's EF6 spatial bridge throws on .NET 5+ (Windows included) without a
backing assembly installed.

Two options are presented:
  1. dotMorten.Microsoft.SqlServer.Types — managed shim, cross-platform.
  2. Microsoft.SqlServer.Types — official package, Windows-only with native
     binaries.

Microsoft.Restier.EntityFramework.Spatial deliberately takes no dependency on
either, so consumers can pick what fits their deployment.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 3 — Final verification

### Task 6: Whole-solution build + spatial test re-run

- [ ] **Step 1: Full solution build**

Run:

```bash
dotnet build RESTier.slnx
```

Expected: clean build, warnings-as-errors honored.

- [ ] **Step 2: Re-run the spatial test project end-to-end**

Run:

```bash
dotnet test test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj --logger "console;verbosity=normal"
```

Expected: every test passes, zero skipped, on whichever TFM (`net8.0` / `net9.0` / `net10.0`) the runner picks.

- [ ] **Step 3: Confirm the integration test seed still degrades gracefully without dotMorten**

The EF6 SpatialPlace seed in `LibraryTestInitializer.cs:208-230` is still inside a try/catch. The test runners that consume it (`Tests.EntityFramework`, `Tests.AspNetCore`) do **not** install dotMorten, so the SpatialPlaces table will remain empty on their EF6 path — exactly as before this change. Spot-check by running:

```bash
dotnet test test/Microsoft.Restier.Tests.EntityFramework/Microsoft.Restier.Tests.EntityFramework.csproj --filter "FullyQualifiedName~LibraryContext" --logger "console;verbosity=minimal"
```

Expected: passes; no exception escapes the LibraryTestInitializer seed.

If you want EF6 spatial seed values to actually persist in those runners, that's a future change — out of scope here. The new docs section already tells consumers how to do it for their own apps.

---

## Self-review checklist

After completing all tasks, verify:

1. **Spec coverage:** Every user request is addressed?
   - "Investigate" — root cause is documented in the plan header.
   - "Propose a solution" — dotMorten chosen; rationale in Architecture.
   - "Add a task to update the documentation about EF6 on .NET 8/9/10 and dotMorten" — Task 5.

2. **Placeholder scan:** Plan contains:
   - One `<DOTMORTEN_VERSION>` placeholder that the executor resolves in Task 1 Step 1 via a concrete `dotnet package search` command. Acceptable — the executor produces it.
   - No "TBD", "TODO", "fill in details", or vague "handle edge cases".

3. **Type / symbol consistency:** All file paths in the plan exist on disk (`test/Microsoft.Restier.Tests.EntityFramework.Spatial/*`, `src/Microsoft.Restier.Docs/guides/extending-restier/spatial-types.mdx`, `LibraryTestInitializer.cs`). The package name `dotMorten.Microsoft.SqlServer.Types` is the actual NuGet ID. The property `SqlServerTypesAvailable` and the test names match the current source.
