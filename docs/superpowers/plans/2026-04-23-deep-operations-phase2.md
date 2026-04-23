# Deep Operations Phase 2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix bugs from Phase 1, implement full deep update semantics, add OData 4.01 entity reference support, and complete the spec test matrix.

**Architecture:** Phase 1 established the extraction + flatten + nav-prop-wiring pipeline for deep insert. Phase 2 fixes correctness bugs, adds deep update child matching (query existing children, classify as insert/update/unlink/delete), implements `@id` entity reference parsing, and fills the remaining test coverage gaps.

**Tech Stack:** .NET 8/9/10, Microsoft.AspNetCore.OData 9.x, Microsoft.OData.Core 8.x, Entity Framework 6 + EF Core, xUnit v3, FluentAssertions

**Spec:** `docs/superpowers/specs/2026-04-22-deep-operations-design.md`
**Phase 1:** `docs/superpowers/plans/2026-04-22-deep-operations.md`

---

## Context: Phase 1 State

Phase 1 delivered:
- `DataModificationItem` tree structure (ParentItem, NestedItems, NavigationBindings, FlattenDepthFirst)
- `DeepOperationExtractor` — walks EdmEntityObject, builds item tree, detects `@odata.bind` via key-subset heuristic
- `EFChangeSetInitializer` — Phase 1 bind validation, Phase 2 nav prop wiring via object assignment
- Controller Post() and Update() — extraction + flatten into ChangeSet
- 8 deep insert + 4 deep update feature tests (EF6 + EFCore)

Phase 1 known issues (from code review):
1. Nested update items always created as `Update` even when no key (should be `Insert`)
2. MaxDepth off-by-one: `currentDepth >= MaxDepth` rejects too early
3. Null nav prop values skipped before nav prop detection (prevents null-unlink)
4. 4.01 entity reference (`@id`) URI parsing not implemented
5. Deep update child matching not implemented (no query existing, no classify, no unlink/delete)
6. Response expansion disabled (NullRef in OData serializer)
7. Test coverage much narrower than spec matrix

---

## File Structure

### Modified Files

| File | Change |
|------|--------|
| `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationExtractor.cs` | Fix bugs #1-3, add `@id` parsing, add null nav prop handling |
| `src/Microsoft.Restier.AspNetCore/RestierController.cs` | Add deep update child matching in Update() |
| `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationResponseBuilder.cs` | Investigate and fix response expansion NullRef |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepInsertTests.cs` | Add remaining deep insert tests from spec |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepUpdateTests.cs` | Add remaining deep update tests from spec |

---

## Task 1: Fix DeepOperationExtractor Bugs

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationExtractor.cs`
- Test: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepInsertTests.cs`

Three bugs to fix in `DeepOperationExtractor`:

### Bug 1: Nested update items always `Update` even without keys

**Location:** `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationExtractor.cs` around line 110

**Current code:**
```csharp
var childItem = new DataModificationItem(
    targetEntitySetName,
    targetEntityType.GetClrType(model),
    clrType,
    isCreation ? RestierEntitySetOperation.Insert : RestierEntitySetOperation.Update,
    isCreation ? null : ExtractKeyValues(nestedEntity, targetEntityType),
    ...
```

**Problem:** When `isCreation` is false (update context), ALL nested entities are created as `Update` operations, even if they have no key (meaning they're new entities to be inserted).

**Fix:** Check if extracted keys are non-empty and non-default. If the nested entity has no key or only default key values (e.g., `Guid.Empty`), create an `Insert` operation instead of `Update`.

- [ ] **Step 1.1: Write a test that exposes the bug**

Add to `DeepInsertTests.cs`:

```csharp
[Fact]
public async Task DeepInsert_SingleLevel_WithMaxDepth5_Succeeds()
{
    // Verify that a simple one-level deep insert works with default MaxDepth=5
    // This also validates the MaxDepth off-by-one fix
    var pubId = UniqueId();
    var payload = new
    {
        Id = pubId,
        Addr = new { Zip = "00000" },
        Books = new[]
        {
            new { Isbn = "5555555555555", Title = "Single Level Book", IsActive = true },
        },
    };

    var postResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
        HttpMethod.Post,
        resource: "/Publishers",
        payload: payload,
        acceptHeader: WebApiConstants.DefaultAcceptHeader,
        serviceCollection: ConfigureServices);

    var postContent = await postResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
    postResponse.StatusCode.Should().Be(HttpStatusCode.Created,
        because: $"single-level deep insert should succeed with default MaxDepth=5. Response: {postContent}");
}
```

- [ ] **Step 1.2: Run the test to verify current behavior**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~DeepInsert_SingleLevel_WithMaxDepth5_Succeeds"`

Expected: Should PASS (this test validates the happy path; the off-by-one test comes next).

### Bug 2: MaxDepth off-by-one

**Location:** `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationExtractor.cs` line 42

**Current code:**
```csharp
if (settings.MaxDepth > 0 && currentDepth >= settings.MaxDepth)
{
    throw new ODataException($"Deep operation exceeds maximum nesting depth of {settings.MaxDepth}.");
}
```

**Problem:** `ExtractNestedItems` is called with `currentDepth=0` for the root entity. When it recurses into a child entity, it passes `currentDepth + 1 = 1`. With `MaxDepth=1`, the check `1 >= 1` is true and it throws — but MaxDepth=1 should allow ONE level of nesting (root -> children). The depth should count levels of nesting, not the number of times `ExtractNestedItems` is called.

**Fix:** The depth check should happen BEFORE recursing into children, not at the start of the method. Move the check inside `ProcessSingleNestedEntity` before the recursive `ExtractNestedItems` call, and increment the meaning: `currentDepth` represents the nesting level of the entity being processed (0 = root, 1 = child, 2 = grandchild). The check should be `currentDepth + 1 >= settings.MaxDepth` before recursing.

Actually, the simplest correct fix: change the initial call to NOT count the root entity. The root entity at depth 0 is not "nesting" — nesting starts at depth 1 (children). So the check should be:

```csharp
if (settings.MaxDepth > 0 && currentDepth > settings.MaxDepth)
{
    throw new ODataException($"Deep operation exceeds maximum nesting depth of {settings.MaxDepth}.");
}
```

Change `>=` to `>`. With MaxDepth=1: root at depth 0 (OK), children at depth 1 (OK, 1 > 1 is false), grandchildren at depth 2 (rejected, 2 > 1 is true).

- [ ] **Step 1.3: Write a test for MaxDepth boundary**

Add to `DeepInsertTests.cs`:

```csharp
[Fact]
public async Task DeepInsert_MaxDepth1_AllowsOneLevel()
{
    // MaxDepth=1 should allow Publisher -> Books but reject Books -> Reviews
    var pubId = UniqueId();
    var payload = new
    {
        Id = pubId,
        Addr = new { Zip = "00000" },
        Books = new[]
        {
            new { Isbn = "6666666666666", Title = "Depth 1 OK Book", IsActive = true },
        },
    };

    var postResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
        HttpMethod.Post,
        resource: "/Publishers",
        payload: payload,
        acceptHeader: WebApiConstants.DefaultAcceptHeader,
        serviceCollection: services =>
        {
            ConfigureServices(services);
            services.AddSingleton(new Core.Submit.DeepOperationSettings { MaxDepth = 1 });
        });

    var postContent = await postResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
    postResponse.StatusCode.Should().Be(HttpStatusCode.Created,
        because: $"MaxDepth=1 should allow one level of nesting. Response: {postContent}");
}
```

- [ ] **Step 1.4: Run the test — it should FAIL with current code**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~DeepInsert_MaxDepth1_AllowsOneLevel"`

Expected: FAIL — the off-by-one causes MaxDepth=1 to reject even a single level of nesting.

### Bug 3: Null nav prop values skipped

**Location:** `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationExtractor.cs` line 49

**Current code:**
```csharp
if (!entity.TryGetPropertyValue(propertyName, out var value) || value is null)
{
    continue;
}
```

**Problem:** Null values are skipped before we check if the property is a navigation property. A null navigation property value (e.g., `"Publisher": null` in 4.01) should be detected and stored as metadata indicating "unlink this relationship." Currently it's silently ignored.

**Fix:** Restructure the loop: first check if the property is a navigation property, then handle null as a special case (add to a new `NullNavigationProperties` list on the parent item, or handle inline). For Phase 2, the simplest approach is to NOT handle null nav props in the extractor at all — instead, leave null-unlink handling to the controller's Update() path where it can build appropriate unlink operations. The extractor's job is to extract PRESENT nested entities, not to detect absent/null ones.

However, the spec says `Publisher: null` in 4.01 and `Publisher@odata.bind: null` in 4.0 should unlink. For 4.0, the `@odata.bind: null` is handled by the OData deserializer differently. For 4.01 inline null, we need to detect it.

**Minimal fix:** Add a `NullNavigationProperties` set to `DataModificationItem` that the extractor populates when it encounters a null nav prop value. The controller can then use this to generate unlink operations.

- [ ] **Step 1.5: Apply all three fixes to DeepOperationExtractor**

Read the current file, then apply:

1. **Bug 1 fix** — In `ProcessSingleNestedEntity`, determine operation based on key presence:
```csharp
// Determine if this is an insert or update based on key presence
var extractedKeys = isCreation ? null : ExtractKeyValues(nestedEntity, targetEntityType);
var hasValidKey = extractedKeys is not null && extractedKeys.Count > 0
    && extractedKeys.Values.All(v => v is not null && !IsDefaultValue(v));
var operation = (isCreation || !hasValidKey)
    ? RestierEntitySetOperation.Insert
    : RestierEntitySetOperation.Update;

var childItem = new DataModificationItem(
    targetEntitySetName,
    targetEntityType.GetClrType(model),
    clrType,
    operation,
    hasValidKey ? extractedKeys : null,
    null,
    nestedEntity.CreatePropertyDictionary(actualEdmType, api, isCreation || !hasValidKey))
```

Add helper:
```csharp
private static bool IsDefaultValue(object value)
{
    if (value is Guid guid) return guid == Guid.Empty;
    if (value is int i) return i == 0;
    if (value is long l) return l == 0;
    if (value is string s) return string.IsNullOrEmpty(s);
    return false;
}
```

2. **Bug 2 fix** — Change `>=` to `>` on the depth check:
```csharp
if (settings.MaxDepth > 0 && currentDepth > settings.MaxDepth)
```

3. **Bug 3 fix** — Add `NullNavigationProperties` to `DataModificationItem` and populate it:

First, add to `src/Microsoft.Restier.Core/Submit/ChangeSetItem.cs`:
```csharp
/// <summary>
/// Gets the set of navigation property names that were explicitly set to null in the payload.
/// Used for relationship unlinking during deep update.
/// </summary>
public ISet<string> NullNavigationProperties { get; } = new HashSet<string>();
```

Then in the extractor, restructure the loop:
```csharp
foreach (var propertyName in entity.GetChangedPropertyNames())
{
    var edmProperty = edmType.FindProperty(propertyName);
    if (edmProperty is not IEdmNavigationProperty navProperty)
    {
        continue; // Not a nav prop — already handled by CreatePropertyDictionary
    }

    var clrPropertyName = EdmClrPropertyMapper.GetClrPropertyName(edmProperty, model);

    if (!entity.TryGetPropertyValue(propertyName, out var value) || value is null)
    {
        // Null nav prop — record for unlink handling
        parentItem.NullNavigationProperties.Add(clrPropertyName);
        continue;
    }

    // ... rest of processing (EdmEntityObject / collection handling)
}
```

- [ ] **Step 1.6: Run all tests**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~DeepInsertTests|FullyQualifiedName~DeepUpdateTests"`

Expected: All tests pass, including the new MaxDepth boundary test.

- [ ] **Step 1.7: Commit**

```bash
git commit -am "fix: DeepOperationExtractor bugs — key detection, MaxDepth off-by-one, null nav props"
```

---

## Task 2: Deep Update Child Matching

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs`
- Modify: `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationExtractor.cs` (or new helper class)

This is the most architecturally complex task. When an Update() payload includes a non-delta collection navigation property, the controller must:

1. Query existing children from the database
2. Match payload items to existing children by key
3. Classify each as insert, update, or omitted
4. Handle omitted children based on containment:
   - Non-contained: remove relationship (clear nav prop, EF resolves to FK null / constraint error)
   - Contained: delete the entity

### Step 2.1: Write the child matching logic

- [ ] Create a new method in `RestierController.cs` or a helper class. The method should:

```csharp
/// <summary>
/// For a deep update, queries existing children for each collection navigation property
/// and classifies nested items as Insert (new), Update (matched by key), or generates
/// unlink/delete items for omitted children.
/// </summary>
private async Task ClassifyDeepUpdateChildren(
    DataModificationItem updateItem,
    IEdmEntitySet entitySet,
    IEdmModel model,
    CancellationToken cancellationToken)
{
    // For each collection nav prop that has nested items:
    var navPropGroups = updateItem.NestedItems
        .GroupBy(n => n.ParentNavigationPropertyName)
        .ToList();

    foreach (var group in navPropGroups)
    {
        var navPropName = group.Key;
        var edmEntityType = entitySet.EntityType();
        var edmNavProp = edmEntityType.FindProperty(navPropName) as IEdmNavigationProperty;
        if (edmNavProp is null || edmNavProp.TargetMultiplicity() == EdmMultiplicity.One
                               || edmNavProp.TargetMultiplicity() == EdmMultiplicity.ZeroOrOne)
        {
            continue; // Single nav props don't need child matching
        }

        // Query existing children
        var targetEntitySet = entitySet.FindNavigationTarget(edmNavProp);
        // Build query: parentEntitySet.Where(key).SelectMany(navProp)
        // Or: targetEntitySet.Where(FK == parentKey)
        // Use api.QueryAsync to get existing children

        // Match by key
        // For each existing child not in the payload:
        //   - Non-contained: generate unlink operation
        //   - Contained: generate delete operation
    }
}
```

This is complex and requires:
- Querying existing children via the API's query pipeline
- Building key comparisons for matching
- Determining containment via `IEdmNavigationProperty.ContainsTarget`
- Generating additional `DataModificationItem` entries for unlink/delete

- [ ] **Step 2.2: Integrate into Update()**

In `RestierController.Update()`, after `ExtractNestedItems` and before `FlattenDepthFirst`:

```csharp
if (updateItem.NestedItems.Count > 0)
{
    await ClassifyDeepUpdateChildren(updateItem, entitySet, model, cancellationToken)
        .ConfigureAwait(false);
}
```

- [ ] **Step 2.3: Write tests**

Add to `DeepUpdateTests.cs`:

```csharp
[Fact]
public async Task DeepUpdate_Patch_CollectionNavProperty_V401()
{
    // First create a publisher with books via deep insert
    // Then PATCH with a modified books collection
    // Verify: new books added, existing books updated, omitted books unlinked
}

[Fact]
public async Task DeepUpdate_Put_OmittedChildrenUnlinked()
{
    // Create publisher with 2 books
    // PUT with only 1 book
    // Verify the omitted book has PublisherId = null (unlinked, not deleted)
}

[Fact]
public async Task DeepUpdate_Put_RequiredRelationship_Returns400()
{
    // PUT that would unlink a child with a required FK
    // Should return 400 from DbUpdateException mapping
}
```

- [ ] **Step 2.4: Run tests, iterate**

- [ ] **Step 2.5: Commit**

```bash
git commit -am "feat: add deep update child matching with insert/update/unlink classification"
```

---

## Task 3: OData 4.01 Entity Reference (`@id`) Support

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationExtractor.cs`
- Test: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepInsertTests.cs`

### Step 3.1: Research how AspNetCore.OData 9.x represents `@id`

- [ ] Write an exploratory test that sends a 4.01 payload with `@id` and inspects what the `EdmEntityObject` looks like after deserialization. Check:
  - Does `TryGetPropertyValue("@id", ...)` work?
  - Does `TryGetPropertyValue("@odata.id", ...)` work?
  - What properties does the `EdmEntityObject` have?
  - Is the `@id` value a URI string that needs parsing?

### Step 3.2: Implement `@id` URI parsing

- [ ] Add a method to `DeepOperationExtractor` that parses an OData entity reference URI into entity set + key:

```csharp
private BindReference ParseEntityReferenceUri(string referenceUri, IEdmNavigationProperty navProperty)
{
    // Parse "Publishers('PUB01')" or "http://host/odata/Publishers('PUB01')"
    // Extract entity set name and key values
    // Use ODataUriParser or manual parsing
}
```

### Step 3.3: Update `IsEntityReference` and `CreateBindReference`

- [ ] Enhance `IsEntityReference` to detect 4.01 entity references:
  - Check for `@id` property (4.01 format)
  - Check for `@odata.id` property (could appear in either version)
  - Parse the URI and create a proper `BindReference` with extracted keys

### Step 3.4: Write tests

Add to `DeepInsertTests.cs`:

```csharp
[Fact]
public async Task DeepInsert_WithEntityReference_V401()
{
    // POST Book with inline Publisher entity-reference using @id
    // OData-Version: 4.01 header
}

[Fact]
public async Task DeepInsert_BindInV401Request_Rejected()
{
    // POST with @odata.bind under OData-Version: 4.01
    // Should return 400
}
```

### Step 3.5: Commit

```bash
git commit -am "feat: add OData 4.01 entity reference (@id) support"
```

---

## Task 4: OData Version Enforcement

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationExtractor.cs`
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs`

### Step 4.1: Reject inline deep update under OData 4.0

- [ ] In the controller's `Update()` method, check the `OData-Version` header. If it's `4.0` and the extractor found any `NestedItems` (non-bind inline entities), return 400 with a message explaining that inline deep update requires OData 4.01.

```csharp
var odataVersion = Request.Headers["OData-Version"].FirstOrDefault();
if (odataVersion == "4.0" && updateItem.NestedItems.Any(n => n.EntitySetOperation != RestierEntitySetOperation.Update))
{
    // 4.0 does not allow inline deep update
}
```

### Step 4.2: Reject `@odata.bind` under OData 4.01

- [ ] In `DeepOperationExtractor`, when detecting `@odata.bind` style references, check the OData version and reject if 4.01.

### Step 4.3: Write tests

```csharp
[Fact]
public async Task DeepUpdate_InlineEntityInV40_Rejected()
{
    // Send OData-Version: 4.0 header with inline nested entity in PATCH
    // Should return 400
}
```

### Step 4.4: Commit

```bash
git commit -am "feat: enforce OData 4.0/4.01 version rules for deep operations"
```

---

## Task 5: Response Expansion Investigation and Fix

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationResponseBuilder.cs`
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs`

### Step 5.1: Investigate the NullRef in SelectedPropertiesNode.Create

- [ ] The NullRef occurs at `SelectedPropertiesNode.<>c.<Create>b__21_0(ExpandedNavigationSelectItem _)`. This lambda is called during `ODataMessageWriterSettings.get_SelectedProperties()`. Investigate:
  - Is the `SelectExpandClause` we build missing required properties?
  - Does `ExpandedNavigationSelectItem` need a non-null `SelectAndExpand` property?
  - Does `NavigationPropertySegment` need a non-null navigation source?
  - Try building the clause with an empty `SelectExpandClause` for child clauses instead of null

### Step 5.2: Try alternative approaches

If setting `SelectExpandClause` on `ODataFeature` doesn't work with `CreatedODataResult`:

- [ ] **Option A:** Return the entity as a loaded EF graph and use `ObjectResult` with custom serialization
- [ ] **Option B:** Re-query the entity with `$expand` after creation and return that result
- [ ] **Option C:** Use `ODataSerializerContext` with `SelectExpandClause` directly

### Step 5.3: Write test

```csharp
[Fact]
public async Task DeepInsert_ResponseIncludesExpandedEntities()
{
    // POST Publisher with inline Books
    // Verify the 201 response body includes the Books in the response (expanded)
}
```

### Step 5.4: Re-enable response shaping in controller or document limitation

### Step 5.5: Commit

```bash
git commit -am "feat: implement response expansion for deep insert/update"
```

---

## Task 6: Complete Test Coverage

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepInsertTests.cs`
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepUpdateTests.cs`

Add all remaining tests from the spec matrix that aren't covered by Tasks 1-5.

### Step 6.1: Add remaining deep insert tests

- [ ] Add to `DeepInsertTests.cs`:

```csharp
[Fact]
public async Task DeepInsert_SingleNavProperty()
// POST Book with inline Publisher (single nav prop, not collection)

[Fact]
public async Task DeepInsert_WithBindReference_V40()
// POST Book with Publisher@odata.bind (explicit 4.0 test)

[Fact]
public async Task DeepInsert_CollectionWithBind_V40()
// POST Publisher with Books@odata.bind array

[Fact]
public async Task DeepInsert_MixedBindAndCreate_V40()
// POST Publisher with some inline Books and some @odata.bind

[Fact]
public async Task DeepInsert_MultiLevel()
// POST Publisher -> Books -> Reviews (2-level nesting)

[Fact]
public async Task DeepInsert_BindReferenceNotFound_Returns400()
// POST with @odata.bind pointing to non-existent entity
// Verify 400 and no partial changes

[Fact]
public async Task DeepInsert_BindDoesNotFireConventionMethods()
// Verify OnInsertingPublisher does NOT fire when Publisher is only bound
```

### Step 6.2: Add remaining deep update tests

- [ ] Add to `DeepUpdateTests.cs`:

```csharp
[Fact]
public async Task DeepUpdate_SingleNavProperty_V401()
// PATCH Book with inline Publisher change (4.01)

[Fact]
public async Task DeepUpdate_EntityRefOnUpdate_V401()
// PATCH Book with Publisher entity-reference @id (4.01)

[Fact]
public async Task DeepUpdate_NullUnlinks_V401()
// PATCH Book with Publisher: null inline (4.01)

[Fact]
public async Task DeepUpdate_NestedDelta_Returns501()
// Returns 501 when nested delta payload detected

[Fact]
public async Task DeepUpdate_FiresConventionMethods()
// OnUpdatingPublisher fires for nested entity update (4.01)
```

### Step 6.3: Run full suite

Run: `dotnet test RESTier.slnx`

### Step 6.4: Commit

```bash
git commit -am "test: complete deep operations test coverage per spec matrix"
```

---

## Task 7: DbUpdateException Error Mapping

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs` or exception handling middleware

### Step 7.1: Add DbUpdateException -> 400 mapping

- [ ] When `SaveChangesAsync` fails due to a required-relationship constraint violation during deep update, the `DbUpdateException` propagates as a 500. Map it to 400 with a descriptive message.

The fix can be in:
- The controller's Post()/Update() with try-catch around `api.SubmitAsync`
- Or a global exception filter

### Step 7.2: Write test

```csharp
[Fact]
public async Task DeepUpdate_Put_RequiredRelationship_Returns400()
// PUT that would unlink a child with required FK — returns 400
```

### Step 7.3: Commit

```bash
git commit -am "fix: map DbUpdateException to HTTP 400 for required-relationship violations"
```

---

## Implementation Notes

### Task Dependencies

Tasks 1 (bug fixes) should be done first — all subsequent tasks depend on correct extraction behavior.

Tasks 2-5 are largely independent and can be done in any order:
- Task 2 (child matching) is the most complex
- Task 3 (4.01 refs) is research-heavy
- Task 4 (version enforcement) is straightforward once Task 3 is done
- Task 5 (response expansion) is isolated investigation

Task 6 (test coverage) should be done last — it depends on all features being implemented.
Task 7 (error mapping) can be done at any point.

### Recommended Order

1. Task 1 (bug fixes) — quick wins, unblocks correct testing
2. Task 7 (error mapping) — small, independent
3. Task 2 (child matching) — largest task, core functionality
4. Task 3 (4.01 refs) — research + implementation
5. Task 4 (version enforcement) — depends on Task 3
6. Task 5 (response expansion) — isolated investigation
7. Task 6 (test coverage) — fill remaining gaps
