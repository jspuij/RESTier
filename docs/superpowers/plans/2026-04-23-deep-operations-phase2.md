# Deep Operations Phase 2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix bugs from Phase 1, implement full deep update semantics, add OData 4.01 entity reference support, and complete the spec test matrix.

**Architecture:** Phase 1 established the extraction + flatten + nav-prop-wiring pipeline for deep insert. Phase 2 fixes correctness bugs, adds deep update child matching (query existing children, classify as insert/update/unlink), implements `@id` entity reference parsing, and fills the remaining test coverage gaps.

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

## Design Contract 1: Entity Reference Parsing

### Accepted shapes

| OData-Version | Format | Example |
|---|---|---|
| 4.0 | `NavProp@odata.bind` annotation | `"Publisher@odata.bind": "Publishers('PUB01')"` |
| 4.0 | `NavProp@odata.bind` array | `"Books@odata.bind": ["Books(guid'...')"]` |
| 4.01 | Inline object with `@id` | `"Publisher": { "@id": "Publishers('PUB01')" }` |
| 4.01 | Inline object with `@odata.id` | `"Publisher": { "@odata.id": "Publishers('PUB01')" }` |

### Version rejection rules

- OData 4.0 requests MUST NOT contain inline deep update entities (only `@odata.bind` and deep insert on POST)
- OData 4.01 requests MUST NOT use `@odata.bind` — use inline entity references with `@id` instead
- If the ASP.NET Core OData formatter rejects `@odata.bind` under 4.01 before the controller sees it, no additional check is needed (Task 1 will determine this)

### Parser choice and construction

Use `Microsoft.OData.UriParser.ODataUriParser` to parse entity reference URIs.

**Construction:**
- Derive service root from the current request's route prefix: `HttpContext.ODataFeature().BaseAddress` or `Request.GetRoutePrefix()` + host
- Pass the IEdmModel from `api.Model`
- For absolute URIs: strip the host and service root prefix to get the relative path, then parse
- For relative URIs: parse directly against the model

**Parsing rules:**
- The parsed path MUST consist of exactly one `EntitySetSegment` followed by one `KeySegment`
- Reject paths with navigation segments, function/action segments, or property segments — these are not valid entity references
- Extract entity set name from `EntitySetSegment.EntitySet.Name`
- Extract key values from `KeySegment.Keys` (an `IEnumerable<KeyValuePair<string, object>>`)

**Handles:**
- Relative URIs: `Publishers('PUB01')`
- Absolute URIs: `http://host/odata/Publishers('PUB01')`
- Composite keys: `OrderItems(OrderId=1,ItemId=2)`

### Output

All accepted entity reference shapes produce a `BindReference`:
```csharp
new BindReference
{
    ResourceSetName = "Publishers",         // from URI path
    ResourceKey = { { "Id", "PUB01" } },   // from key segment
}
```

### Detection in extractor

Phase 1's key-subset heuristic (changed properties are a subset of key properties) works for `@odata.bind` under 4.0 because the deserializer creates a synthetic `EdmEntityObject` with only key values.

For 4.01 `@id`, the detection depends on Task 1 exploration: does the deserializer set an `@id` property on the `EdmEntityObject`, or does it produce a different structure? The parser implementation adapts to the actual deserializer output.

---

## Design Contract 2: Relationship Operation Contract

### Scope constraint for Phase 2

Phase 2 supports relationships where:
- The dependent entity has an **explicit FK scalar property** (e.g., `Book.PublisherId`)
- The FK is **nullable** (for unlinking) or **required** (produces 400 on unlink attempt)
- The relationship is discoverable via `IEdmNavigationProperty` on the EDM model

Phase 2 does NOT support:
- Many-to-many relationships (skip navigations, join tables)
- Shadow FK properties (EF Core only, no CLR scalar property)
- Navigation-only models without any FK property

These constraints are enforced by the classifier: if the request semantics require classification/unlinking and the inverse FK property cannot be found, the classifier **rejects the request with 501 Not Implemented**, not a silent skip. A client sending a complete relationship set in a PUT expects all omitted children to be unlinked — silently skipping would turn a full PUT into a partial update. The 501 response should include a message like "Deep update for navigation property '{name}' is not supported: no explicit foreign key property found."

### How to query existing children

For a collection nav prop on a parent entity (e.g., `Publisher.Books`):

1. Get the parent entity's key from the URL path (already available as `RestierQueryBuilder.GetPathKeyValues(path, model)`)
2. Get the target entity set from `entitySet.FindNavigationTarget(edmNavProp)`
3. Find the inverse FK property name:
   - Get the partner navigation property: `edmNavProp.Partner` gives the inverse nav on the target type
   - Get the referential constraint: `edmNavProp.ReferentialConstraint` or `edmNavProp.Partner.ReferentialConstraint` maps dependent property to principal property
   - The dependent property name in the constraint is the FK property name on the child entity
4. Query: `api.GetQueryableSource(targetEntitySetName).Where(fkProp == parentKey)`

```csharp
// Example: Publisher.Books navigation
// edmNavProp = Publisher.Books (type: Collection(Book))
// edmNavProp.Partner = Book.Publisher (type: Publisher)  
// edmNavProp.Partner.ReferentialConstraint = { Book.PublisherId -> Publisher.Id }
// FK property on child = "PublisherId"
// Query: Books.Where(b => b.PublisherId == "PUB01")
```

If `ReferentialConstraint` is null (no explicit FK in the EDM model), fall back to convention: `{NavPropertyName}Id` (e.g., `Publisher` nav prop → `PublisherId` FK). If that property doesn't exist on the CLR type, **reject with 501** (see scope constraint above).

### How to match payload children to existing children

Compare by key properties from the EDM entity type:
1. Get key property names from `targetEntityType.Key()`
2. Map to CLR names via `EdmClrPropertyMapper.GetClrPropertyName`
3. For each payload child's `ResourceKey`, find an existing child where all key values match
4. Use `Convert.ChangeType` for type coercion (OData may send `int` for a `long` key)

### Relationship removal representation

**For non-contained collection nav props (unlink):**

Use nav property clearing in the EF initializers rather than FK scalar injection. This avoids the inverse-FK-discovery problem and works with any relationship shape EF supports.

Representation: a new `RelationshipRemoval` metadata class stored on the parent `DataModificationItem`. It stores entity set + key (NOT a live entity instance), analogous to `BindReference`. The EF initializer resolves it during Phase 1 (same as bind validation) to ensure consistent DbContext tracking lifetime.

```csharp
public class RelationshipRemoval
{
    /// The navigation property name on the parent entity.
    public string NavigationPropertyName { get; set; }
    
    /// The target entity set name (for querying the child entity).
    public string ResourceSetName { get; set; }
    
    /// The key of the child entity to unlink.
    public IReadOnlyDictionary<string, object> ResourceKey { get; set; }
    
    /// The resolved child entity instance (populated during EF initializer Phase 1,
    /// same tracking context as other entities). Null until resolved.
    public object ResolvedEntity { get; set; }
}
```

The `DataModificationItem` gets a new property:
```csharp
public IList<RelationshipRemoval> RelationshipRemovals { get; } = new List<RelationshipRemoval>();
```

**Resolution and execution in EF initializers:**

Phase 1 (before entity materialization): Resolve each `RelationshipRemoval` by querying the entity by key, same as `BindReference` resolution. Store the tracked entity instance on `ResolvedEntity`. This ensures the resolved instance is in the same `DbContext` tracking context as the parent entity.

Phase 2 (after parent entity materialization): For each resolved removal:
- Collection nav prop: find the `ResolvedEntity` in the parent's collection by key comparison (NOT object identity — use key matching), then remove it
- Single nav prop: set the parent's nav property to null

EF's change tracker resolves the FK change. Key-based removal avoids the IList.Remove object-identity problem.

**For contained nav props (delete):**

Create a `DataModificationItem` with `EntitySetOperation = Delete` and `ResourceKey` = child's key. This uses the existing delete pipeline.

**For single nav prop null (`Publisher: null` or `Publisher@odata.bind: null`):**

Same as collection unlink: add a `RelationshipRemoval` with nav prop name and the current related entity's key. The classifier queries the root entity with `$expand={navProp}` to discover the current related entity's key (see "How to load current single nav prop" below).

### How to load current single nav prop for unlink

When the classifier needs to unlink an existing single nav relationship:

1. Query: `api.GetQueryableSource(rootEntitySetName).Where(key).Select(e => e.{NavProp})`
2. Or simpler: `api.GetQueryableSource(rootEntitySetName).Where(key)` then inspect the result's nav prop after EF loads it
3. Extract the related entity's key
4. Store as `RelationshipRemoval { NavigationPropertyName, ResourceSetName (of the related entity), ResourceKey }`

If the nav prop is currently null (no existing relationship), no removal is needed.

### How to handle single nav props in classification

The classifier MUST handle single nav props:
- Payload has key matching existing related entity → reclassify as `Update`
- Payload has key NOT matching existing related entity → `Insert` + unlink old (add `RelationshipRemoval` for old)
- Payload has no key → `Insert` + unlink old
- Payload is null → unlink only (no insert)
- Payload is entity reference → already handled as `NavigationBinding`

---

## Recommended Task Order

1. **Task 1: Exploratory — Deserializer shape** (learn, don't commit tests)
2. **Task 2: Extractor bug fixes** (depth, null nav props, key preservation)
3. **Task 3: Entity reference parsing** (implement @id/@odata.id + bind tests)
4. **Task 4: OData version plumbing** (enforce 4.0/4.01 rules)
5. **Task 5: Deep update classification** (query existing, classify, relationship removal)
6. **Task 6: DbUpdateException error mapping** (narrow to relationship violations)
7. **Task 7: Response expansion investigation**
8. **Task 8: Remaining test coverage**

---

## Task 1: Exploratory — Deserializer Shape for Entity References

**Purpose:** Before changing the extractor, learn exactly what AspNetCore.OData 9.x gives us. Do NOT commit these tests — document findings and convert meaningful behaviors into permanent tests in Task 3.

### Step 1.1: Write local exploratory tests

- [ ] Add temporary logging to `DeepOperationExtractor.ExtractNestedItems` (or use a debugger) to capture for each payload:
  - `edmEntityObject.GetChangedPropertyNames()` — what property names appear?
  - Type of each value — `EdmEntityObject`? `string`? `IEnumerable`?
  - `TryGetPropertyValue("@id", ...)` and `TryGetPropertyValue("@odata.id", ...)` results
  - Whether the `@odata.bind` annotation shows up as a changed property name

Test payloads:
1. `POST /Books` with `"Publisher@odata.bind": "Publishers('Publisher1')"` (4.0)
2. `POST /Publishers` with `"Books@odata.bind": ["Books(guid'...')"]` (4.0)
3. `POST /Books` with `"Publisher": { "@id": "Publishers('Publisher1')" }` (4.01)
4. `POST /Books` with `"Publisher@odata.bind": "Publishers('Publisher1')"` (4.01)

### Step 1.2: Document findings

- [ ] Record in a comment or temporary file:
  - For each payload: what `GetChangedPropertyNames()` returns
  - How the deserializer represents `@odata.bind` (is it a changed property? an annotation?)
  - How `@id` appears on the `EdmEntityObject`
  - Whether the formatter itself rejects `@odata.bind` under 4.01

### Step 1.3: No commit — findings inform Tasks 3 and 4

---

## Task 2: Extractor Bug Fixes

**Files:**
- Modify: `src/Microsoft.Restier.Core/Submit/ChangeSetItem.cs`
- Modify: `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationExtractor.cs`
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepInsertTests.cs`

### Bug 1: MaxDepth off-by-one

**Fix:** Check depth BEFORE recursing into children. If adding this child would create a tree deeper than `MaxDepth`, **throw** (not silently return). The child has already been added to `NestedItems` at this point, so if we detect that the child itself has nested content that would exceed depth, we reject the entire request.

- [ ] **Step 2.1: Write failing test**

Add to `DeepInsertTests.cs`:

```csharp
[Fact]
public async Task DeepInsert_MaxDepth1_AllowsOneLevel()
{
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

- [ ] **Step 2.2: Fix depth check**

Remove the depth check from the top of `ExtractNestedItems`. Add it in `ProcessSingleNestedEntity` BEFORE creating or adding the child:

```csharp
// Compute the depth this child would be at
var childDepth = currentDepth + 1;

// Check if this child would exceed max depth AND has nested nav values
// (a child at max depth is allowed if it has no grandchildren;
// reject the entire request if it would require over-depth processing)
if (settings.MaxDepth > 0 && childDepth >= settings.MaxDepth
    && HasNestedNavigationValues(nestedEntity, actualEdmType))
{
    throw new ODataException(
        $"Deep operation exceeds maximum nesting depth of {settings.MaxDepth}.");
}

// Safe to add the child — it either has no grandchildren or is within depth
var childItem = new DataModificationItem(...);
parentItem.NestedItems.Add(childItem);

// Only recurse if within depth
if (settings.MaxDepth == 0 || childDepth < settings.MaxDepth)
{
    ExtractNestedItems(nestedEntity, actualEdmType, childItem, isCreation, childDepth);
}
```

This ensures: the depth check happens BEFORE the child is added, no mutated state on exception, and a child at the max depth boundary is allowed if it has no nested nav content.

Add helper:
```csharp
private bool HasNestedNavigationValues(Delta entity, IEdmStructuredType edmType)
{
    foreach (var propertyName in entity.GetChangedPropertyNames())
    {
        if (!entity.TryGetPropertyValue(propertyName, out var value) || value is null)
            continue;
        var edmProperty = edmType.FindProperty(propertyName);
        if (edmProperty is IEdmNavigationProperty && (value is EdmEntityObject || (value is IEnumerable && value is not string)))
            return true;
    }
    return false;
}
```

- [ ] **Step 2.3: Verify both MaxDepth tests pass**

Run: `dotnet test ... --filter "MaxDepth"`
Expected: Both `DeepInsert_MaxDepth1_AllowsOneLevel` (PASS) and `DeepInsert_ExceedsMaxDepth_Returns400` (PASS).

### Bug 2: Null nav prop values skipped

- [ ] **Step 2.4: Add NullNavigationProperties to DataModificationItem**

In `src/Microsoft.Restier.Core/Submit/ChangeSetItem.cs`:

```csharp
/// <summary>
/// Navigation property names explicitly set to null in the payload.
/// Used for relationship unlinking during deep update.
/// </summary>
public ISet<string> NullNavigationProperties { get; } = new HashSet<string>();
```

- [ ] **Step 2.5: Restructure extractor loop**

Move nav prop detection before null check (see Design Contract 2 for the restructured loop).

### Bug 3: Extractor should preserve raw keys, not classify

- [ ] **Step 2.6: Always use `Insert` for nested entities**

Change `ProcessSingleNestedEntity` to always create `RestierEntitySetOperation.Insert` items with extracted keys preserved in `ResourceKey`. The classifier (Task 5) reclassifies based on existing children.

- [ ] **Step 2.7: Run all tests, commit**

```bash
git commit -am "fix: MaxDepth off-by-one, null nav prop detection, raw key preservation"
```

---

## Task 3: Entity Reference Parsing + Bind Tests

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationExtractor.cs`
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepInsertTests.cs`

Based on Task 1 findings, implement proper entity reference detection and URI parsing.

### Step 3.1: Write failing tests first

- [ ] Add to `DeepInsertTests.cs`:

```csharp
[Fact]
public async Task DeepInsert_WithBindReference_V40()
{
    // POST Book with Publisher@odata.bind (explicit 4.0 test)
    // Verify publisher is linked, not created
}

[Fact]
public async Task DeepInsert_CollectionWithBind_V40()
{
    // POST Publisher with Books@odata.bind array
    // Verify existing books are linked to the new publisher
}

[Fact]
public async Task DeepInsert_BindReferenceNotFound_Returns400()
{
    // POST with @odata.bind pointing to non-existent entity
    // Verify 400 and no partial changes (atomicity)
}

[Fact]
public async Task DeepInsert_WithEntityReference_V401()
{
    // POST Book with inline Publisher entity-reference using @id
    // OData-Version: 4.01 header
}
```

### Step 3.2: Implement entity reference URI parsing

- [ ] Add to `DeepOperationExtractor`:

```csharp
private BindReference ParseEntityReferenceUri(string referenceUri, IEdmNavigationProperty navProperty)
{
    // Use ODataUriParser to parse the URI
    // Extract entity set name from the path
    // Extract key values from key segment
    // Return BindReference with ResourceSetName and ResourceKey
}
```

### Step 3.3: Update IsEntityReference and CreateBindReference

- [ ] Adapt based on Task 1 findings. For `@id` under 4.01:
  - Check `TryGetPropertyValue("@id", out var idValue)` or `TryGetPropertyValue("@odata.id", out var idValue)`
  - If found, parse the URI string via `ParseEntityReferenceUri`
  - Create `BindReference` from parsed result

### Step 3.4: Run tests, commit

```bash
git commit -am "feat: implement entity reference detection and URI parsing with bind tests"
```

---

## Task 4: OData Version Plumbing

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationExtractor.cs`
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs`
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepUpdateTests.cs`

### Step 4.1: Add OData version to extractor constructor

- [ ] ```csharp
public DeepOperationExtractor(IEdmModel model, ApiBase api, DeepOperationSettings settings, string odataVersion = null)
```

Controller passes `Request.Headers["OData-Version"].FirstOrDefault()`.

### Step 4.2: Normalize OData version with safe default

- [ ] OData 4.0 is the conservative default when the header is missing. Normalize once:

```csharp
var rawVersion = Request.Headers["OData-Version"].FirstOrDefault()
    ?? Request.Headers["OData-MaxVersion"].FirstOrDefault();
var odataVersion = string.IsNullOrEmpty(rawVersion) ? "4.0" : rawVersion;
```

### Step 4.3: Reject inline deep update under 4.0

- [ ] In `RestierController.Update()`, after extraction:

```csharp
if (odataVersion == "4.0" && updateItem.NestedItems.Count > 0)
{
    return BadRequest("Inline deep update requires OData-Version: 4.01. Use @odata.bind for 4.0.");
}
```

### Step 4.3: Write failing test, implement, verify

- [ ] Add to `DeepUpdateTests.cs`:

```csharp
[Fact]
public async Task DeepUpdate_InlineEntityInV40_Rejected()
{
    // Send OData-Version: 4.0 header with inline nested entity in PATCH
    // Should return 400
}
```

### Step 4.4: Handle @odata.bind under 4.01

Based on Task 1 findings — implement rejection or document that the formatter handles it.

### Step 4.5: Commit

```bash
git commit -am "feat: enforce OData 4.0/4.01 version rules for deep operations"
```

---

## Task 5: Deep Update Classification

The most complex task. Uses both Design Contracts above.

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore/Submit/DeepUpdateClassifier.cs`
- Modify: `src/Microsoft.Restier.Core/Submit/ChangeSetItem.cs` (add `RelationshipRemoval`, `RelationshipRemovals`)
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs`
- Modify: `src/Microsoft.Restier.EntityFrameworkCore/Submit/EFChangeSetInitializer.cs`
- Modify: `src/Microsoft.Restier.EntityFramework/Submit/EFChangeSetInitializer.cs`
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepUpdateTests.cs`

### Step 5.1: Write failing tests first

- [ ] Add to `DeepUpdateTests.cs`:

```csharp
[Fact]
public async Task DeepUpdate_InlineNewChildWithoutKey_Inserts()
{
    // Create publisher, PATCH with Books containing a new book (no Id)
    // Assert new book is inserted and linked
}

[Fact]
public async Task DeepUpdate_Put_OmittedChildrenUnlinked()
{
    // Create publisher with 2 books
    // PUT with only 1 book
    // Assert omitted book still exists but has PublisherId = null
}

[Fact]
public async Task DeepUpdate_NullNavProperty_Unlinks_V401()
{
    // PATCH Book with Publisher: null (4.01 inline null)
    // Assert publisher is unlinked
}
```

### Step 5.2: Add RelationshipRemoval to DataModificationItem

- [ ] In `src/Microsoft.Restier.Core/Submit/ChangeSetItem.cs`, add the `RelationshipRemoval` class and property. `RelationshipRemoval` stores entity set + key (NOT live entity instances) — resolved by EF initializer Phase 1 in the same tracking context:

```csharp
/// <summary>
/// Represents a relationship to be removed during deep update.
/// Stores entity set + key; resolved by EF initializer Phase 1.
/// </summary>
public class RelationshipRemoval
{
    /// <summary>
    /// The navigation property name on the parent entity to clear.
    /// </summary>
    public string NavigationPropertyName { get; set; }

    /// <summary>
    /// The target entity set name (for querying the child entity).
    /// </summary>
    public string ResourceSetName { get; set; }

    /// <summary>
    /// The key of the child entity to unlink.
    /// </summary>
    public IReadOnlyDictionary<string, object> ResourceKey { get; set; }

    /// <summary>
    /// The resolved child entity instance (set during EF initializer Phase 1).
    /// Same tracking context as other entities. Null until resolved.
    /// </summary>
    public object ResolvedEntity { get; set; }
}
```

Add to `DataModificationItem`:
```csharp
/// <summary>
/// Relationship removals to process during deep update.
/// </summary>
public IList<RelationshipRemoval> RelationshipRemovals { get; } = new List<RelationshipRemoval>();
```

### Step 5.3: Create DeepUpdateClassifier

- [ ] Create `src/Microsoft.Restier.AspNetCore/Submit/DeepUpdateClassifier.cs`:

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
        var edmEntityType = entitySet.EntityType();

        // Split nested items by nav prop multiplicity
        var groups = rootItem.NestedItems
            .GroupBy(n => n.ParentNavigationPropertyName)
            .ToList();

        foreach (var group in groups)
        {
            var navPropName = group.Key;
            var edmNavProp = edmEntityType.FindProperty(navPropName) as IEdmNavigationProperty;
            if (edmNavProp is null) continue;

            if (edmNavProp.TargetMultiplicity() == EdmMultiplicity.Many)
            {
                await ClassifyCollectionNavProp(
                    rootItem, navPropName, group.ToList(),
                    edmNavProp, edmEntityType, entitySet, isFullReplace, cancellationToken);
            }
            else
            {
                // Single nav prop (ZeroOrOne or One) — exactly one nested item expected
                await ClassifySingleNavProp(
                    rootItem, navPropName, group.First(),
                    edmNavProp, edmEntityType, entitySet, cancellationToken);
            }
        }

        // Handle NullNavigationProperties (explicit null for unlink)
        foreach (var nullNavProp in rootItem.NullNavigationProperties)
        {
            await HandleNullNavProp(rootItem, nullNavProp, edmEntityType, entitySet, cancellationToken);
        }
    }

    private async Task ClassifySingleNavProp(
        DataModificationItem rootItem,
        string navPropName,
        DataModificationItem payloadItem,
        IEdmNavigationProperty edmNavProp,
        IEdmEntityType edmEntityType,
        IEdmEntitySet entitySet,
        CancellationToken cancellationToken)
    {
        // Load current related entity to determine if we need to unlink the old one
        var currentRelated = await LoadCurrentSingleNavProp(
            rootItem, navPropName, edmNavProp, entitySet, cancellationToken);

        if (payloadItem.ResourceKey is not null && payloadItem.ResourceKey.Count > 0)
        {
            // Has key — check if it matches current related entity
            if (currentRelated is not null && KeysMatch(currentRelated, payloadItem.ResourceKey, edmNavProp.ToEntityType()))
            {
                // Same entity — reclassify as Update
                payloadItem.EntitySetOperation = RestierEntitySetOperation.Update;
            }
            else
            {
                // Different entity or no current — keep as Insert
                // Unlink old if exists
                if (currentRelated is not null)
                {
                    AddRelationshipRemoval(rootItem, navPropName, currentRelated, edmNavProp, entitySet);
                }
            }
        }
        else
        {
            // No key — new entity to Insert, unlink old if exists
            if (currentRelated is not null)
            {
                AddRelationshipRemoval(rootItem, navPropName, currentRelated, edmNavProp, entitySet);
            }
        }
    }

    private async Task HandleNullNavProp(
        DataModificationItem rootItem,
        string navPropName,
        IEdmEntityType edmEntityType,
        IEdmEntitySet entitySet,
        CancellationToken cancellationToken)
    {
        var edmNavProp = edmEntityType.FindProperty(navPropName) as IEdmNavigationProperty;
        if (edmNavProp is null) return;

        // Load current related entity
        var currentRelated = await LoadCurrentSingleNavProp(
            rootItem, navPropName, edmNavProp, entitySet, cancellationToken);

        if (currentRelated is not null)
        {
            AddRelationshipRemoval(rootItem, navPropName, currentRelated, edmNavProp, entitySet);
        }
    }

    private async Task<object> LoadCurrentSingleNavProp(
        DataModificationItem rootItem,
        string navPropName,
        IEdmNavigationProperty edmNavProp,
        IEdmEntitySet entitySet,
        CancellationToken cancellationToken)
    {
        // Query: rootEntitySet.Where(key) — then inspect nav prop
        var query = api.GetQueryableSource(rootItem.ResourceSetName);
        // Apply key filter from rootItem.ResourceKey
        // ... (same pattern as FindResource in EFChangeSetInitializer)
        // Load with Include/Expand for the nav prop
        // Return the related entity or null
        // Implementation detail: may need to use .Select(e => e.{NavProp}) 
        // or load root and read nav prop via reflection
    }

    private void AddRelationshipRemoval(
        DataModificationItem rootItem,
        string navPropName,
        object currentRelatedEntity,
        IEdmNavigationProperty edmNavProp,
        IEdmEntitySet entitySet)
    {
        var targetEntitySet = entitySet.FindNavigationTarget(edmNavProp);
        var targetEntityType = edmNavProp.ToEntityType();
        var key = DefaultChangeSetInitializer.GetKeyValues(currentRelatedEntity, targetEntityType, model);

        rootItem.RelationshipRemovals.Add(new RelationshipRemoval
        {
            NavigationPropertyName = navPropName,
            ResourceSetName = targetEntitySet?.Name ?? edmNavProp.ToEntityType().Name,
            ResourceKey = key,
        });
    }
}
```

### Step 5.4: Implement collection nav prop classification

Following Design Contract 2:

```csharp
private async Task ClassifyCollectionNavProp(
    DataModificationItem rootItem,
    string navPropName,
    List<DataModificationItem> payloadItems,
    IEdmNavigationProperty edmNavProp,
    IEdmEntityType edmEntityType,
    IEdmEntitySet entitySet,
    bool isFullReplace,
    CancellationToken cancellationToken)
{
    // Find inverse FK via referential constraint
    var fkPropertyName = FindInverseFkPropertyName(edmNavProp);
    if (fkPropertyName is null)
    {
        // Cannot determine FK — reject if request semantics require classification
        if (isFullReplace || payloadItems.Any(p => p.ResourceKey?.Count > 0))
        {
            throw new StatusCodeException(HttpStatusCode.NotImplemented,
                $"Deep update for navigation property '{navPropName}' is not supported: " +
                $"no explicit foreign key property found.");
        }
        return; // Insert-only deep insert — no classification needed
    }

    // Get parent key
    var parentKeyValues = rootItem.ResourceKey;
    if (parentKeyValues is null || parentKeyValues.Count == 0) return;

    // Query existing children: targetEntitySet.Where(FK == parentKey)
    var targetEntitySet = entitySet.FindNavigationTarget(edmNavProp);
    var existingChildren = await QueryChildrenByFk(
        targetEntitySet.Name, fkPropertyName, parentKeyValues, cancellationToken);

    // Classify payload items
    var targetEntityType = edmNavProp.ToEntityType();
    foreach (var payloadItem in payloadItems)
    {
        if (payloadItem.ResourceKey is not null && payloadItem.ResourceKey.Count > 0)
        {
            var matched = FindMatchingChild(existingChildren, payloadItem.ResourceKey, targetEntityType);
            if (matched is not null)
            {
                payloadItem.EntitySetOperation = RestierEntitySetOperation.Update;
            }
            // else: has key but not currently related — keep as Insert
        }
        // else: no key — keep as Insert
    }

    // Handle omitted children (PUT replace semantics)
    if (isFullReplace)
    {
        var payloadKeySet = payloadItems
            .Where(p => p.ResourceKey is not null && p.ResourceKey.Count > 0)
            .Select(p => p.ResourceKey)
            .ToList();

        foreach (var existing in existingChildren)
        {
            if (!IsInPayload(existing, payloadKeySet, targetEntityType))
            {
                if (edmNavProp.ContainsTarget)
                {
                    // Contained: delete
                    var deleteItem = CreateDeleteItem(existing, targetEntitySet.Name, targetEntityType);
                    rootItem.NestedItems.Add(deleteItem);
                }
                else
                {
                    // Non-contained: relationship removal (key-based, resolved by EF initializer)
                    var targetType = edmNavProp.ToEntityType();
                    var childKey = DefaultChangeSetInitializer.GetKeyValues(existing, targetType, model);
                    rootItem.RelationshipRemovals.Add(new RelationshipRemoval
                    {
                        NavigationPropertyName = navPropName,
                        ResourceSetName = targetEntitySet.Name,
                        ResourceKey = childKey,
                    });
                }
            }
        }
    }
}
```

### Step 5.5: Update EF initializers to resolve and process RelationshipRemovals

- [ ] In both `EFChangeSetInitializer.InitializeAsync`:

**Phase 1 addition (alongside BindReference resolution):** Resolve each `RelationshipRemoval` by querying the entity by key. Reuse the existing `ResolveBindReference` logic. This ensures the resolved instance is in the same DbContext tracking context.

```csharp
// Phase 1: also resolve RelationshipRemovals
foreach (var entry in context.ChangeSet.Entries.OfType<DataModificationItem>())
{
    foreach (var removal in entry.RelationshipRemovals)
    {
        var bindRef = new BindReference
        {
            ResourceSetName = removal.ResourceSetName,
            ResourceKey = removal.ResourceKey,
        };
        // Reuse ResolveBindReference but don't throw if not found
        // (entity may have been deleted by a concurrent operation)
        try
        {
            removal.ResolvedEntity = await ResolveBindReference(context, bindRef, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (StatusCodeException)
        {
            // Entity no longer exists — skip this removal
        }
    }
}
```

**Phase 2 addition (after parent materialization):** Process removals using key-based matching (NOT object identity):

```csharp
// Process relationship removals
if (entry.RelationshipRemovals.Count > 0 && entry.Resource is not null)
{
    foreach (var removal in entry.RelationshipRemovals)
    {
        if (removal.ResolvedEntity is null) continue;

        var navPropInfo = entry.Resource.GetType().GetProperty(removal.NavigationPropertyName);
        if (navPropInfo is null) continue;

        if (typeof(IEnumerable).IsAssignableFrom(navPropInfo.PropertyType)
            && navPropInfo.PropertyType != typeof(string))
        {
            // Collection: find by key comparison and remove (not object identity)
            var collection = navPropInfo.GetValue(entry.Resource);
            if (collection is IList list)
            {
                var toRemove = FindByKeyInList(list, removal.ResourceKey);
                if (toRemove is not null)
                {
                    list.Remove(toRemove);
                }
            }
        }
        else
        {
            // Single: set to null
            navPropInfo.SetValue(entry.Resource, null);
        }
    }
}
```

Add helper `FindByKeyInList` that iterates the list and compares key properties:
```csharp
private static object FindByKeyInList(IList list, IReadOnlyDictionary<string, object> key)
{
    foreach (var item in list)
    {
        var allMatch = true;
        foreach (var kvp in key)
        {
            var prop = item.GetType().GetProperty(kvp.Key);
            if (prop is null || !Equals(prop.GetValue(item), kvp.Value))
            {
                allMatch = false;
                break;
            }
        }
        if (allMatch) return item;
    }
    return null;
}
```

### Step 5.6: Integrate classifier into controller

- [ ] In `RestierController.Update()`, after extraction:

```csharp
if (updateItem.NestedItems.Count > 0 
    || updateItem.NullNavigationProperties.Count > 0
    || updateItem.NavigationBindings.Count > 0)
{
    var classifier = new DeepUpdateClassifier(api, model);
    await classifier.ClassifyAsync(updateItem, entitySet, isFullReplaceUpdate, cancellationToken);
}
```

### Step 5.7: Run tests, iterate, commit

```bash
git commit -am "feat: deep update child matching with classification and relationship removal"
```

---

## Task 6: DbUpdateException Error Mapping

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs`

### Step 6.1: Add narrow exception mapping

- [ ] Map only known EF constraint violation exceptions to 400. Preserve 500 for unknown database failures.

```csharp
try
{
    var result = await api.SubmitAsync(changeSet, cancellationToken).ConfigureAwait(false);
}
catch (Exception ex) when (IsRelationshipConstraintViolation(ex))
{
    return BadRequest($"A relationship constraint was violated: {ex.GetBaseException().Message}");
}
// Other exceptions propagate as 500

private static bool IsRelationshipConstraintViolation(Exception ex)
{
    // Check for EFCore DbUpdateException with FK constraint inner exception
    if (ex is Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
    {
        var inner = dbEx.GetBaseException();
        return inner.Message.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase)
            || inner.Message.Contains("REFERENCE", StringComparison.OrdinalIgnoreCase);
    }
    // Check for EF6 DbUpdateException
    if (ex.GetType().FullName == "System.Data.Entity.Infrastructure.DbUpdateException")
    {
        var inner = ex.GetBaseException();
        return inner.Message.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase)
            || inner.Message.Contains("REFERENCE", StringComparison.OrdinalIgnoreCase);
    }
    return false;
}
```

### Step 6.2: Write test, commit

```bash
git commit -am "fix: map relationship constraint DbUpdateException to HTTP 400"
```

---

## Task 7: Response Expansion Investigation

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationResponseBuilder.cs`
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs`

### Step 7.1: Investigate NullRef

- [ ] The NullRef is in `SelectedPropertiesNode.Create` processing `ExpandedNavigationSelectItem`. Try in order:
  1. Non-null empty child clause: `new SelectExpandClause(Enumerable.Empty<SelectItem>(), true)` instead of `null` for leaf nodes
  2. Verify `NavigationSource` on `ExpandedNavigationSelectItem` is non-null — log what `entitySet.FindNavigationTarget(edmNavProp)` returns
  3. Verify `ODataExpandPath` has the correct segment structure (single `NavigationPropertySegment`)
  4. Try `new ExpandedNavigationSelectItem(path, navigationSource, new SelectExpandClause(Array.Empty<SelectItem>(), true))` with all non-null params

### Step 7.2: Write acceptance tests (required before declaring success)

- [ ] These tests define what "response expansion works" means. All must pass:

```csharp
[Fact]
public async Task DeepInsert_ResponseIncludesExpandedBooks()
{
    // POST Publisher with inline Books
    // Deserialize the 201 response body (not a follow-up GET)
    // Assert response.Books is not null and has correct count
}

[Fact]
public async Task DeepInsert_ResponseIncludesMultiLevelExpand()
{
    // POST Publisher with Books containing Reviews (2-level)
    // Assert 201 response includes Publisher.Books[].Reviews
}

[Fact]
public async Task DeepInsert_ResponseIncludesBoundEntities()
{
    // POST Book with Publisher@odata.bind
    // Assert 201 response includes the bound Publisher in the response
}
```

### Step 7.3: If fix works, re-enable in controller, run acceptance tests, commit

### Step 7.4: If fix doesn't work after reasonable investigation

- [ ] Document as a known limitation. Add a note to the spec that clients should `GET {entity-url}?$expand=...` after deep insert for the expanded response. Remove `DeepOperationResponseBuilder` from the codebase (dead code) or keep it with a TODO for when OData.NET fixes the serializer behavior.

### Step 7.5: Commit

```bash
git commit -am "feat: response expansion (or document limitation with acceptance test expectations)"
```

---

## Task 8: Remaining Test Coverage

Add remaining tests from spec matrix not covered by Tasks 2-7.

### Deep insert gaps:
- `DeepInsert_SingleNavProperty` — POST Book with inline Publisher
- `DeepInsert_MixedBindAndCreate_V40` — some inline + some @odata.bind
- `DeepInsert_MultiLevel` — Publisher -> Books -> Reviews (2-level)
- `DeepInsert_BindDoesNotFireConventionMethods`

### Deep update gaps:
- `DeepUpdate_SingleNavProperty_V401` — PATCH Book with inline Publisher
- `DeepUpdate_EntityRefOnUpdate_V401` — PATCH with @id reference
- `DeepUpdate_NestedDelta_Returns501`
- `DeepUpdate_FiresConventionMethods_V401`

### Commit

```bash
git commit -am "test: complete deep operations test coverage per spec matrix"
```
