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
- 4 distinct deep insert tests + 2 distinct deep update tests in base classes (each runs on both EF6 and EFCore across 3 TFMs = 36 total test passes)

Phase 1 known issues (from code review):
1. Nested update items always created as `Update` even when no key (should be `Insert`)
2. MaxDepth off-by-one: `currentDepth >= MaxDepth` rejects too early
3. Null nav prop values skipped before nav prop detection (prevents null-unlink)
4. 4.01 entity reference (`@id`) URI parsing not implemented
5. Deep update child matching not implemented (no query existing, no classify, no unlink/delete)
6. Response expansion disabled (NullRef in OData serializer)
7. Test coverage narrower than spec matrix

---

## Recommended Task Order

The @id/@odata.bind deserializer shape affects the extractor design, so learning that first reduces churn:

1. **Task 1: Exploratory — Deserializer shape for entity references** (learn what the extractor receives)
2. **Task 2: Extractor bug fixes** (depth, null nav props, key preservation)
3. **Task 3: OData version plumbing** (pass version to extractor, enforce rules)
4. **Task 4: Deep update classification** (the big one — query existing, classify, generate operations)
5. **Task 5: DbUpdateException error mapping**
6. **Task 6: Response expansion investigation**
7. **Task 7: Remaining test coverage**

---

## Task 1: Exploratory — Deserializer Shape for Entity References

**Purpose:** Before changing the extractor, learn exactly what AspNetCore.OData 9.x gives us for:
- `@odata.bind` under OData-Version 4.0
- `@odata.bind` under OData-Version 4.01 (does the formatter reject it? preserve an annotation? produce the same key-subset object?)
- `@id` under OData-Version 4.01
- inline entity reference object under 4.01

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EntityReferenceExploratoryTests.cs` (temporary — may be deleted or converted to permanent tests)

### Step 1.1: Write exploratory tests

- [ ] Create tests that send payloads and log what the controller receives. Use a custom middleware or add temporary logging to `DeepOperationExtractor` to capture:
  - `edmEntityObject.GetChangedPropertyNames()` output
  - Type of each property value (EdmEntityObject? string? other?)
  - Any instance annotations present
  - Whether `TryGetPropertyValue("@id", ...)` or `TryGetPropertyValue("@odata.id", ...)` returns anything

Test payloads to try:

```json
// 4.0 single bind
POST /Books with OData-Version: 4.0
{ "Title": "...", "Publisher@odata.bind": "Publishers('Publisher1')" }

// 4.01 entity reference
POST /Books with OData-Version: 4.01
{ "Title": "...", "Publisher": { "@id": "Publishers('Publisher1')" } }

// 4.0 collection bind
POST /Publishers with OData-Version: 4.0
{ "Id": "...", "Books@odata.bind": ["Books(guid'...')"] }

// 4.01 @odata.bind (should this be rejected by formatter or passed through?)
POST /Books with OData-Version: 4.01
{ "Title": "...", "Publisher@odata.bind": "Publishers('Publisher1')" }
```

### Step 1.2: Document findings

- [ ] Record what each payload produces at the `EdmEntityObject` level. This determines:
  - Whether `IsEntityReference` detection needs to change
  - Whether we need URI parsing or just key extraction
  - Whether 4.01 enforcement happens at the formatter level or needs extractor checks
  - What `@odata.bind` looks like under 4.01

### Step 1.3: Commit findings

```bash
git commit -am "test: exploratory tests for entity reference deserializer behavior"
```

---

## Task 2: Extractor Bug Fixes

**Files:**
- Modify: `src/Microsoft.Restier.Core/Submit/ChangeSetItem.cs`
- Modify: `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationExtractor.cs`
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepInsertTests.cs`
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepUpdateTests.cs`

Three fixes, each with a failing test written first.

### Bug 1: MaxDepth off-by-one

**Model:** `currentDepth` = nesting depth of the entity being processed (root = 0). Check BEFORE adding a child: reject when `currentDepth + 1 > MaxDepth`. This avoids temporarily adding an over-depth child before throwing.

- [ ] **Step 2.1: Write failing test**

Add to `DeepInsertTests.cs`:

```csharp
[Fact]
public async Task DeepInsert_MaxDepth1_AllowsOneLevel()
{
    // MaxDepth=1 should allow Publisher -> Books (1 level of nesting)
    // but reject Books -> Reviews (2 levels)
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

- [ ] **Step 2.2: Verify it fails with current code**

Run: `dotnet test ... --filter "DeepInsert_MaxDepth1_AllowsOneLevel"`
Expected: FAIL (off-by-one rejects even 1 level)

- [ ] **Step 2.3: Fix depth check**

In `DeepOperationExtractor.ExtractNestedItems`, move the depth check to BEFORE recursing into children. In `ProcessSingleNestedEntity`, before the recursive `ExtractNestedItems` call:

```csharp
// Check depth BEFORE recursing into child's children
if (settings.MaxDepth > 0 && currentDepth + 1 > settings.MaxDepth)
{
    // Don't recurse — this child is at the max depth, no grandchildren allowed
    // But the child itself is allowed
    return;
}
ExtractNestedItems(nestedEntity, actualEdmType, childItem, isCreation, currentDepth + 1);
```

Remove the depth check from the top of `ExtractNestedItems` — it's now in `ProcessSingleNestedEntity` only.

- [ ] **Step 2.4: Verify fix**

Run the test again. Expected: PASS.

### Bug 2: Null nav prop values skipped

- [ ] **Step 2.5: Add NullNavigationProperties to DataModificationItem**

In `src/Microsoft.Restier.Core/Submit/ChangeSetItem.cs`, add to `DataModificationItem`:

```csharp
/// <summary>
/// Gets the set of navigation property names explicitly set to null in the payload.
/// Used for relationship unlinking during deep update.
/// </summary>
public ISet<string> NullNavigationProperties { get; } = new HashSet<string>();
```

- [ ] **Step 2.6: Update extractor to detect null nav props**

In `DeepOperationExtractor.ExtractNestedItems`, restructure the loop so nav prop detection happens BEFORE null check:

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
        // Null nav prop — record for unlink handling by the controller/initializer
        parentItem.NullNavigationProperties.Add(clrPropertyName);
        continue;
    }

    var targetEntityType = navProperty.ToEntityType();
    var targetEntitySet = FindTargetEntitySet(navProperty);

    if (value is EdmEntityObject nestedEntity)
    {
        ProcessSingleNestedEntity(...);
    }
    else if (value is IEnumerable collection && value is not string)
    {
        foreach (var item in collection)
        {
            if (item is EdmEntityObject collectionEntity)
            {
                ProcessSingleNestedEntity(...);
            }
        }
    }
}
```

### Bug 3: Extractor should preserve raw key info, not classify insert/update

The extractor should NOT determine whether a nested item in an update context is an `Insert` or `Update`. That decision requires querying existing children (Task 4). The extractor should only preserve what it has: the nested entity's scalar properties and key values. Classification happens in the controller.

- [ ] **Step 2.7: Change extractor to always use `Insert` for nested entities**

In `ProcessSingleNestedEntity`, always use `RestierEntitySetOperation.Insert` and always extract key values (if present). Store extracted keys in `ResourceKey` regardless. The controller's classification step (Task 4) will re-classify based on existing children.

```csharp
var extractedKeys = ExtractKeyValues(nestedEntity, targetEntityType);

var childItem = new DataModificationItem(
    targetEntitySetName,
    targetEntityType.GetClrType(model),
    clrType,
    RestierEntitySetOperation.Insert, // Always Insert — controller reclassifies in Task 4
    extractedKeys.Count > 0 ? extractedKeys : null,
    null,
    nestedEntity.CreatePropertyDictionary(actualEdmType, api, true)) // isCreation=true for LocalValues
{
    ParentItem = parentItem,
    ParentNavigationPropertyName = clrPropertyName,
};
```

- [ ] **Step 2.8: Run all tests**

Run: `dotnet test ... --filter "DeepInsertTests|DeepUpdateTests"`
Expected: All existing + new tests pass.

- [ ] **Step 2.9: Commit**

```bash
git commit -am "fix: MaxDepth off-by-one, null nav prop detection, raw key preservation in extractor"
```

---

## Task 3: OData Version Plumbing

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationExtractor.cs`
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs`

### Step 3.1: Add OData version to extractor

- [ ] Add an `ODataVersion` parameter to the `DeepOperationExtractor` constructor (or an options object). The controller reads the version from `Request.Headers["OData-Version"]` and passes it.

```csharp
internal class DeepOperationExtractor
{
    private readonly IEdmModel model;
    private readonly ApiBase api;
    private readonly DeepOperationSettings settings;
    private readonly string odataVersion; // "4.0" or "4.01"

    public DeepOperationExtractor(IEdmModel model, ApiBase api, DeepOperationSettings settings, string odataVersion = null)
    {
        // ...
        this.odataVersion = odataVersion;
    }
```

### Step 3.2: Reject inline deep update under OData 4.0

- [ ] In `RestierController.Update()`, after extraction, check version:

```csharp
var odataVersion = Request.Headers["OData-Version"].FirstOrDefault();
if (odataVersion == "4.0" && updateItem.NestedItems.Count > 0)
{
    return BadRequest("Inline deep update is not supported under OData 4.0. Use @odata.bind for relationship operations, or send OData-Version: 4.01.");
}
```

Note: the check is `NestedItems.Count > 0` — any inline nested entity is rejected under 4.0, regardless of operation classification.

### Step 3.3: Write failing test first

- [ ] Add to `DeepUpdateTests.cs`:

```csharp
[Fact]
public async Task DeepUpdate_InlineEntityInV40_Rejected()
{
    // Send OData-Version: 4.0 header with inline nested entity in PATCH
    // Should return 400
    // (need to figure out how to set OData-Version header via ExecuteTestRequest)
}
```

Note: `RestierTestHelpers.ExecuteTestRequest` may not expose OData-Version header setting. If not, this test may need a custom `HttpRequestMessage`. Check the Breakdance API.

### Step 3.4: Handle `@odata.bind` under 4.01

Based on Task 1 findings:
- If the formatter rejects `@odata.bind` under 4.01 before the controller sees it: no action needed
- If it passes through: add a check in the extractor that rejects `@odata.bind`-style references when `odataVersion == "4.01"`
- Document the behavior based on Task 1 exploration

### Step 3.5: Commit

```bash
git commit -am "feat: add OData version plumbing and enforce 4.0/4.01 rules"
```

---

## Task 4: Deep Update Child Matching

This is the most complex task. It requires a concrete design for how to represent relationship operations.

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore/Submit/DeepUpdateClassifier.cs`
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs`
- Modify: `src/Microsoft.Restier.EntityFrameworkCore/Submit/EFChangeSetInitializer.cs`
- Modify: `src/Microsoft.Restier.EntityFramework/Submit/EFChangeSetInitializer.cs`
- Add tests to: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepUpdateTests.cs`

### Design: Relationship Removal Representation

When a non-contained child is omitted from a PUT collection, the relationship must be removed. Two approaches:

**Approach A: Update items that null the inverse FK.**
For each omitted child, create a `DataModificationItem` with `EntitySetOperation = Update`, `ResourceKey` = child's key, and `LocalValues` = `{ "PublisherId": null }`. The existing EF pipeline handles this as a normal update that nulls the FK.

**Approach B: Metadata on the parent item.**
Add a `RelationshipRemovals` collection to `DataModificationItem` listing nav prop + child keys to unlink. EF initializers process these by clearing nav props.

**Decision: Approach A** — it requires no new item types, uses the existing pipeline, fires `OnUpdating*` conventions for the affected children (correct: the child IS being updated — its FK is changing), and EF's change tracker handles the rest. The initializer already knows how to process Update items.

For contained children, use `EntitySetOperation = Delete` items.

### Step 4.1: Write failing tests first

- [ ] Add to `DeepUpdateTests.cs`:

```csharp
[Fact]
public async Task DeepUpdate_InlineNewChildWithoutKey_Inserts()
{
    // Create a publisher, then PATCH/PUT with a Books array containing
    // a new book (no Id or Id=Guid.Empty). The new book should be inserted.
    var pubId = UniqueId();
    // First create the publisher
    var createPayload = new { Id = pubId, Addr = new { Zip = "00000" } };
    var createResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
        HttpMethod.Post,
        resource: "/Publishers",
        payload: createPayload,
        acceptHeader: WebApiConstants.DefaultAcceptHeader,
        serviceCollection: ConfigureServices);
    createResponse.IsSuccessStatusCode.Should().BeTrue();

    // Now PATCH with an inline book (no key = new entity to insert)
    var patchPayload = new
    {
        Books = new[]
        {
            new { Isbn = "7777777777777", Title = "New Inline Book", IsActive = true },
        },
    };

    var patchResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
        new HttpMethod("PATCH"),
        resource: $"/Publishers('{pubId}')",
        payload: patchPayload,
        acceptHeader: WebApiConstants.DefaultAcceptHeader,
        serviceCollection: ConfigureServices);

    var content = await patchResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
    patchResponse.IsSuccessStatusCode.Should().BeTrue(
        because: $"inline new book should be inserted. Response: {content}");

    // Verify the book was created
    var getResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
        HttpMethod.Get,
        resource: $"/Publishers('{pubId}')?$expand=Books",
        acceptHeader: ODataConstants.DefaultAcceptHeader,
        serviceCollection: ConfigureServices);
    var (publisher, _) = await getResponse.DeserializeResponseAsync<Publisher>();
    publisher.Books.Should().HaveCount(1);
    publisher.Books[0].Title.Should().Be("New Inline Book");
}

[Fact]
public async Task DeepUpdate_Put_OmittedChildrenUnlinked()
{
    // Create publisher with 2 books via deep insert
    // PUT with only 1 book
    // Verify the omitted book has PublisherId = null (unlinked, not deleted)
    var pubId = UniqueId();
    var createPayload = new
    {
        Id = pubId,
        Addr = new { Zip = "00000" },
        Books = new[]
        {
            new { Isbn = "8888888888881", Title = "Keep This Book", IsActive = true },
            new { Isbn = "8888888888882", Title = "Unlink This Book", IsActive = true },
        },
    };

    var createResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
        HttpMethod.Post,
        resource: "/Publishers",
        payload: createPayload,
        acceptHeader: WebApiConstants.DefaultAcceptHeader,
        serviceCollection: ConfigureServices);
    createResponse.IsSuccessStatusCode.Should().BeTrue();

    // Get the books to know their IDs
    var getResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
        HttpMethod.Get,
        resource: $"/Publishers('{pubId}')?$expand=Books",
        acceptHeader: ODataConstants.DefaultAcceptHeader,
        serviceCollection: ConfigureServices);
    var (publisher, _) = await getResponse.DeserializeResponseAsync<Publisher>();
    publisher.Books.Should().HaveCount(2);
    var keepBook = publisher.Books.First(b => b.Title == "Keep This Book");
    var unlinkBook = publisher.Books.First(b => b.Title == "Unlink This Book");

    // PUT with only the "keep" book — omitting the "unlink" book
    var putPayload = new
    {
        Id = pubId,
        Addr = new { Zip = "00000" },
        LastUpdated = publisher.LastUpdated,
        Books = new[]
        {
            new { Id = keepBook.Id, Isbn = keepBook.Isbn, Title = keepBook.Title, IsActive = keepBook.IsActive },
        },
    };

    var putResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
        HttpMethod.Put,
        resource: $"/Publishers('{pubId}')",
        payload: putPayload,
        acceptHeader: WebApiConstants.DefaultAcceptHeader,
        serviceCollection: ConfigureServices);

    var putContent = await putResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
    putResponse.IsSuccessStatusCode.Should().BeTrue(
        because: $"PUT should succeed. Response: {putContent}");

    // Verify: the unlinked book still exists but has no publisher
    var unlinkCheckResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
        HttpMethod.Get,
        resource: $"/Books({unlinkBook.Id})?$expand=Publisher",
        acceptHeader: ODataConstants.DefaultAcceptHeader,
        serviceCollection: ConfigureServices);
    var (updatedUnlinkBook, _) = await unlinkCheckResponse.DeserializeResponseAsync<Book>();
    updatedUnlinkBook.Should().NotBeNull("the book should still exist (not deleted)");
    updatedUnlinkBook.Publisher.Should().BeNull("the book should be unlinked from the publisher");
}
```

### Step 4.2: Implement DeepUpdateClassifier

- [ ] Create `src/Microsoft.Restier.AspNetCore/Submit/DeepUpdateClassifier.cs`:

This class takes a root `DataModificationItem` (from extraction) and reclassifies nested items by comparing against existing children from the database.

```csharp
internal class DeepUpdateClassifier
{
    private readonly ApiBase api;
    private readonly IEdmModel model;

    public async Task ClassifyAsync(
        DataModificationItem rootItem,
        IEdmEntitySet entitySet,
        bool isFullReplace,
        CancellationToken cancellationToken)
    {
        // Group nested items by navigation property
        var groups = rootItem.NestedItems
            .GroupBy(n => n.ParentNavigationPropertyName)
            .ToList();

        foreach (var group in groups)
        {
            var navPropName = group.Key;
            var edmNavProp = entitySet.EntityType().FindProperty(navPropName) as IEdmNavigationProperty;
            if (edmNavProp is null) continue;

            // Skip single nav props — only collections need child matching
            if (edmNavProp.TargetMultiplicity() != EdmMultiplicity.Many) continue;

            // Query existing children
            var existingChildren = await QueryExistingChildren(
                rootItem, navPropName, edmNavProp, entitySet, cancellationToken);

            var payloadItems = group.ToList();

            // Match payload items to existing children by key
            foreach (var payloadItem in payloadItems)
            {
                if (payloadItem.ResourceKey is null || payloadItem.ResourceKey.Count == 0)
                {
                    // No key — this is a new entity, keep as Insert
                    continue;
                }

                // Check if any existing child matches by key
                var matched = FindMatchingChild(existingChildren, payloadItem.ResourceKey);
                if (matched is not null)
                {
                    // Existing child matched — reclassify as Update
                    payloadItem.EntitySetOperation = RestierEntitySetOperation.Update;
                }
                // else: has key but not currently related — Insert (link new)
            }

            // Handle omitted children (existing but not in payload)
            if (isFullReplace) // PUT semantics
            {
                var payloadKeys = payloadItems
                    .Where(p => p.ResourceKey is not null && p.ResourceKey.Count > 0)
                    .Select(p => p.ResourceKey)
                    .ToList();

                foreach (var existing in existingChildren)
                {
                    if (!IsInPayload(existing, payloadKeys))
                    {
                        // Omitted child
                        if (edmNavProp.ContainsTarget)
                        {
                            // Contained: delete
                            var deleteItem = CreateDeleteItem(existing, ...);
                            rootItem.NestedItems.Add(deleteItem);
                        }
                        else
                        {
                            // Non-contained: unlink by nulling the inverse FK
                            var unlinkItem = CreateUnlinkItem(existing, edmNavProp, ...);
                            rootItem.NestedItems.Add(unlinkItem);
                        }
                    }
                }
            }
        }

        // Also handle NullNavigationProperties
        foreach (var nullNavProp in rootItem.NullNavigationProperties)
        {
            // Generate unlink operation for the current relationship
            // (clear the nav prop reference on the root entity)
        }
    }
}
```

The `CreateUnlinkItem` method creates a `DataModificationItem` with:
- `EntitySetOperation = Update`
- `ResourceKey` = the child's key
- `LocalValues` = `{ inverseFkPropertyName: null }`
- `ResourceSetName` = the child's entity set name

This reuses the existing update pipeline — EF's `SetValues` will set the FK to null.

### Step 4.3: Integrate into controller

- [ ] In `RestierController.Update()`, after extraction:

```csharp
if (updateItem.NestedItems.Count > 0 || updateItem.NullNavigationProperties.Count > 0
    || updateItem.NavigationBindings.Count > 0)
{
    var classifier = new DeepUpdateClassifier(api, model);
    await classifier.ClassifyAsync(updateItem, entitySet, isFullReplaceUpdate, cancellationToken);
}
```

### Step 4.4: Run tests

Expected: `DeepUpdate_InlineNewChildWithoutKey_Inserts` and `DeepUpdate_Put_OmittedChildrenUnlinked` should pass.

### Step 4.5: Commit

```bash
git commit -am "feat: add deep update child matching with insert/update/unlink classification"
```

---

## Task 5: DbUpdateException Error Mapping

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs`

### Step 5.1: Write failing test

- [ ] Add to `DeepUpdateTests.cs`:

```csharp
[Fact]
public async Task DeepUpdate_Put_RequiredRelationship_Returns400()
{
    // Create a scenario where unlinking a child would violate a required FK constraint
    // The response should be 400, not 500
    // (This requires a model with a required FK — Review.BookId is required)
}
```

### Step 5.2: Add try-catch around SubmitAsync

- [ ] In both `Post()` and `Update()`, wrap `api.SubmitAsync()` in try-catch for `DbUpdateException`:

```csharp
try
{
    var result = await api.SubmitAsync(changeSet, cancellationToken).ConfigureAwait(false);
}
catch (Exception ex) when (IsConstraintViolation(ex))
{
    return BadRequest($"A relationship constraint was violated: {ex.GetBaseException().Message}");
}
```

The `IsConstraintViolation` helper checks for `DbUpdateException` (EFCore) or `System.Data.Entity.Infrastructure.DbUpdateException` (EF6) with an inner exception indicating a constraint violation.

### Step 5.3: Commit

```bash
git commit -am "fix: map DbUpdateException to HTTP 400 for constraint violations"
```

---

## Task 6: Response Expansion Investigation

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationResponseBuilder.cs`
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs`

### Step 6.1: Investigate the NullRef

- [ ] The NullRef is at `SelectedPropertiesNode.<>c.<Create>b__21_0(ExpandedNavigationSelectItem _)`. Investigate:
  - Does `ExpandedNavigationSelectItem` need a non-null `SelectAndExpand` property? Try passing `new SelectExpandClause(Enumerable.Empty<SelectItem>(), true)` instead of `null` for leaf nodes.
  - Does the `NavigationSource` on the `ExpandedNavigationSelectItem` need to be non-null?
  - Does the `ODataExpandPath` need additional segments?

### Step 6.2: Try fix

- [ ] If the NullRef is caused by null `SelectAndExpand`, change `DeepOperationResponseBuilder` to always provide a non-null (empty) child clause:

```csharp
var childClause = childClauseFromRecursion ?? new SelectExpandClause(Enumerable.Empty<SelectItem>(), true);
```

### Step 6.3: If fix works, re-enable in controller

- [ ] Remove the TODO comments and re-enable the `SelectExpandClause` assignment in Post() and Update().

### Step 6.4: Write test

```csharp
[Fact]
public async Task DeepInsert_ResponseIncludesExpandedEntities()
{
    // POST Publisher with inline Books
    // Deserialize the 201 response
    // Verify the response body includes Books (expanded)
}
```

### Step 6.5: If fix doesn't work, try alternative approaches

- [ ] **Option A:** Re-query the entity with `$expand` after creation and return that
- [ ] **Option B:** Use `ObjectResult` with custom serialization context
- [ ] **Option C:** Document as a known limitation with a workaround (GET with $expand)

### Step 6.6: Commit

```bash
git commit -am "feat: implement response expansion for deep insert/update (or document limitation)"
```

---

## Task 7: Remaining Test Coverage

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepInsertTests.cs`
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepUpdateTests.cs`

Add remaining tests from the spec matrix not already covered by Tasks 1-6.

### Step 7.1: Deep insert gap tests

- [ ] Add:

```csharp
DeepInsert_SingleNavProperty        // POST Book with inline Publisher (single, not collection)
DeepInsert_WithBindReference_V40    // POST Book with Publisher@odata.bind (explicit 4.0)
DeepInsert_CollectionWithBind_V40   // POST Publisher with Books@odata.bind array
DeepInsert_MixedBindAndCreate_V40   // POST Publisher with some inline + some @odata.bind
DeepInsert_MultiLevel               // POST Publisher -> Books -> Reviews (2-level)
DeepInsert_BindReferenceNotFound    // @odata.bind to non-existent entity -> 400, no partial changes
DeepInsert_BindDoesNotFireConventionMethods  // OnInserting* does NOT fire for bound entity
```

### Step 7.2: Deep update gap tests

- [ ] Add:

```csharp
DeepUpdate_SingleNavProperty_V401       // PATCH Book with inline Publisher (4.01)
DeepUpdate_EntityRefOnUpdate_V401       // PATCH Book with Publisher @id (4.01)
DeepUpdate_NullUnlinks_V401             // PATCH Book with Publisher: null inline (4.01)
DeepUpdate_NestedDelta_Returns501       // Nested delta payload -> 501
DeepUpdate_FiresConventionMethods_V401  // OnUpdating* fires for nested entity (4.01)
```

### Step 7.3: Run full suite

```bash
dotnet test RESTier.slnx
```

### Step 7.4: Commit

```bash
git commit -am "test: complete deep operations test coverage per spec matrix"
```
