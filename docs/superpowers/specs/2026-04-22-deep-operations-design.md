# Deep Operations Design: Deep Insert, Deep Update, and @odata.bind

**Date**: 2026-04-22
**Issue**: [OData/RESTier#646](https://github.com/OData/RESTier/issues/646)
**Status**: Draft

## Overview

RESTier currently silently ignores navigation properties in POST/PUT/PATCH payloads (`Extensions.cs:122-127`). This design adds support for:

- **Deep insert** (OData 4.0 section 11.4.2.2): Creating related entities inline during POST
- **Deep update** (OData 4.01 section 11.4.3.1): Updating related entities inline during PATCH/PUT
- **`@odata.bind`** (OData 4.0 section 11.4.2.1): Linking to existing entities by reference

## Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Convention pipeline | Full pipeline for all nested entities | Preserves RESTier's core interception contract |
| `@odata.bind` resolution | Hybrid: FK assignment + existence validation | Simple and performant, with good error messages |
| Nesting depth | Configurable, default 5 | Recursive implementation with safety guard |
| PATCH collection semantics | Merge (upsert) | OData 4.01 recommended, non-destructive |
| PUT collection semantics | Replace | Standard HTTP PUT full-replacement semantics |
| Architecture | Flatten nested entities into ChangeSet | Each nested entity is a first-class DataModificationItem |

## Architecture

### Approach: Flatten into ChangeSet

Nested entities in the OData payload are recursively extracted into individual `DataModificationItem` entries. Each entry flows through the full RESTier submit pipeline (authorization, validation, pre/post events). Items are enqueued in dependency order (parent before children) so FKs can be wired after parent materialization.

```
HTTP POST /Publishers
{
  "Id": "PUB01",
  "Books": [
    { "Title": "Book A", "Isbn": "1234567890123" },
    { "Title": "Book B", "Isbn": "9876543210123" }
  ]
}

  Extraction                          ChangeSet Queue
  ──────────                          ────────────────
  EdmEntityObject (Publisher)    →    1. Insert Publisher (PUB01)
    ├─ EdmEntityObject (Book A)  →    2. Insert Book A (ParentItem → #1)
    └─ EdmEntityObject (Book B)  →    3. Insert Book B (ParentItem → #1)

  After #1 is materialized:
    #2.LocalValues["PublisherId"] = "PUB01"  (FK wired)
    #3.LocalValues["PublisherId"] = "PUB01"  (FK wired)
```

### Why not delegate to EF's change tracker?

Delegating the full object graph to EF would be simpler, but nested entities would bypass RESTier's convention pipeline — `OnInsertingBook()`, `OnValidatingBook()`, etc. would not fire for nested entities. This violates RESTier's core contract.

## Component Changes

### 1. Core Data Model (`Microsoft.Restier.Core`)

#### `DataModificationItem` — new properties

```csharp
/// The parent DataModificationItem (null for root/direct operations).
public DataModificationItem ParentItem { get; set; }

/// The CLR navigation property name on the parent entity.
public string ParentNavigationPropertyName { get; set; }

/// Child items created by deep insert/update extraction.
public IList<DataModificationItem> NestedItems { get; }

/// True when this item represents an @odata.bind reference
/// (link to existing entity, not create/update).
public bool IsBindOperation { get; set; }
```

`NestedItems` is used during extraction to build the tree. `ParentItem` is used during initialization to wire FKs. Both are null/empty for non-nested operations, so fully backward compatible.

#### `DeepOperationSettings` — new configuration class

```csharp
public class DeepOperationSettings
{
    /// Maximum nesting depth. Default: 5. Set to 0 to disable deep operations.
    public int MaxDepth { get; set; } = 5;
}
```

#### `BindReferenceValidator` — new validator

Implements `IChainedService<IChangeSetItemValidator>`. During the validation phase, for each `DataModificationItem` where `IsBindOperation == true`:
1. Query the target entity set using `ResourceKey`
2. If not found, add `ChangeSetItemValidationResult` with `Severity = Error`
3. Produces HTTP 400 with descriptive validation error

Chains with existing `ConventionBasedChangeSetItemValidator` via `IChainOfResponsibilityFactory<IChangeSetItemValidator>`.

#### `DefaultChangeSetInitializer` — new protected helpers

Add protected helper methods for FK resolution that both EF6 and EFCore initializers can call:

- `GetForeignKeyPropertyName(IEdmModel, IEdmNavigationProperty)` — resolves the FK property name from the EDM navigation relationship
- `GetKeyValues(DataModificationItem)` — reads the materialized entity's key values via reflection

These are provider-agnostic (EDM model and reflection only).

### 2. Nested Entity Extraction (`Microsoft.Restier.AspNetCore`)

#### New class: `DeepOperationExtractor`

Responsible for walking an `EdmEntityObject` and building a tree of `DataModificationItem` entries.

**Input**: Root `EdmEntityObject`, `IEdmStructuredType`, `IEdmModel`, `ApiBase`, `DeepOperationSettings`, operation type (insert/update), and for updates: `isFullReplaceUpdate` flag.

**Process**:
1. Call `CreatePropertyDictionary` for scalar/complex properties (existing behavior)
2. Walk changed properties, identify navigation properties via EDM type
3. For each navigation property value:
   - **`EdmEntityObject` (single nav, deep insert/update)**: Recursively extract, create child `DataModificationItem` with `ParentItem` set
   - **Collection of `EdmEntityObject` (collection nav)**: Process each item in the collection
   - **`@odata.bind` reference**: Parse entity set and key from the bind URI, create `DataModificationItem` with `IsBindOperation = true` and `ResourceKey` set to the parsed key
4. Track current depth, throw `ODataException` (HTTP 400) if `MaxDepth` is exceeded

**Output**: Root `DataModificationItem` with `NestedItems` tree populated.

**Detecting `@odata.bind` vs deep insert**: AspNetCore.OData's `ODataResourceDeserializer` handles `@odata.bind` by creating synthetic resources with key values from the reference URI. The exact detection mechanism (e.g., `ODataIdAnnotation` on instance annotations, or checking if only key properties are present) needs to be verified against AspNetCore.OData 9.x's deserialization output during implementation. The extractor must reliably distinguish bind references from full nested entities.

#### `Extensions.cs` — `CreatePropertyDictionary` changes

The existing method continues to build `LocalValues` for scalar and complex properties only. The `EdmEntityObject` skip (`continue` on line 126) remains — navigation properties are handled separately by `DeepOperationExtractor`, not mixed into `LocalValues`.

### 3. Controller Changes (`Microsoft.Restier.AspNetCore`)

#### `RestierController.Post()`

After creating the root `DataModificationItem`:
1. Call `DeepOperationExtractor.Extract()` to build the nested item tree
2. Flatten the tree (depth-first pre-order, guaranteeing parent before children) into an ordered list
3. Enqueue all items into the `ChangeSet`

```csharp
var postItem = new DataModificationItem(...);
var extractor = new DeepOperationExtractor(model, api, deepOperationSettings);
extractor.ExtractNestedItems(edmEntityObject, actualEntityType, postItem, isCreation: true);

var changeSet = new ChangeSet();
foreach (var item in postItem.FlattenDepthFirst())
{
    changeSet.Entries.Enqueue(item);
}
var result = await api.SubmitAsync(changeSet, cancellationToken);
```

Batch support: when `HttpContext.GetChangeSet()` is non-null, items are enqueued into the shared batch changeset in the same order.

#### `RestierController.Update()`

Same extraction, plus deep update logic:

**For collection navigation properties**:
- Query existing children via `api.QueryAsync()`
- Match payload items to existing children by key
- Create `Insert` items for new entities, `Update` items for matched entities
- For PUT (`isFullReplaceUpdate`): create `Delete` items for existing children not in payload

**For single navigation properties**:
- Nested entity with matching key → `Update`
- Nested entity with new/no key → `Insert` (unlink previous if needed)
- `@odata.bind` → set FK to referenced key
- `null` → set FK to null (unlink)
- Absent from payload → no action (PATCH)

**Ordering in ChangeSet for deep update**:
1. Root update item
2. Child inserts (parent FK already known from URL key)
3. Child updates
4. Child deletes (last, to avoid FK constraint violations)

### 4. ChangeSet Initialization (`EntityFramework` / `EntityFrameworkCore`)

Both `EFChangeSetInitializer` implementations process items sequentially from the ChangeSet queue (existing behavior). The additions:

**After materializing each item** — if `entry.ParentItem != null`:
1. Call base class helper to resolve the FK property name from the EDM navigation relationship
2. Read the parent's key value from `entry.ParentItem.Resource` (already materialized, since parent was enqueued first)
3. Create a new `LocalValues` dictionary that includes the FK (since `LocalValues` is `IReadOnlyDictionary`, the initializer builds a new dictionary from the original plus the FK entry, and replaces `LocalValues` on the item — this requires adding a setter or an internal `SetLocalValues` method to `DataModificationItem`)

**For `@odata.bind` items** (`entry.IsBindOperation == true`):
- **Single nav prop bind** (e.g., `Publisher@odata.bind` on a Book): Set the FK property on the parent entity's tracked entry. The parent is already materialized.
- **Collection nav prop bind** (e.g., `Books@odata.bind` on a Publisher): Load the referenced entity via `FindResource()`, set its FK to point to the parent.

**Provider-specific differences** (why these are not in the shared project):
- EFCore: `dbContext.Entry(resource)` returns `EntityEntry`, FK set via `EntityEntry.Property(fkName).CurrentValue`
- EF6: `dbContext.Entry(resource)` returns `DbEntityEntry`, FK set via `DbEntityEntry.Property(fkName).CurrentValue`

### 5. DI Registration

#### `RestierODataOptionsExtensions`

Register `DeepOperationSettings` alongside existing `ODataQuerySettings`:

```csharp
services.AddSingleton(new DeepOperationSettings());
```

Add builder method:
```csharp
public static RestierApiBuilder ConfigureDeepOperations(
    this RestierApiBuilder builder, Action<DeepOperationSettings> configure)
```

#### `ServiceCollectionExtensions` (EF Shared)

Register `BindReferenceValidator` in the validator chain:
```csharp
.AddSingleton<IChainedService<IChangeSetItemValidator>, BindReferenceValidator>()
```

## Test Strategy

### Test Model Changes

**New entity**: `Review` in `Tests.Shared/Scenarios/Library/`

```csharp
public class Review
{
    public Guid Id { get; set; }
    public string Content { get; set; }
    public int Rating { get; set; }
    public Guid BookId { get; set; }
    public Book Book { get; set; }
}
```

**Modified entity**: `Book` — add explicit FK and Reviews collection:

```csharp
// Add to Book.cs:
public string PublisherId { get; set; }
public virtual ObservableCollection<Review> Reviews { get; set; }
```

**Modified context**: `LibraryContext` — add `DbSet<Review> Reviews`.

**Seed data**: Add sample Reviews in `LibraryTestInitializer`.

No migrations needed — the test database is recreated via `EnsureDeleted()` + `EnsureCreated()`.

### Unit Tests

| Test Class | Project | Covers |
|-----------|---------|--------|
| `DeepOperationExtractorTests` | `Tests.AspNetCore` | Nested entity extraction from EdmEntityObject, @odata.bind parsing, depth limit enforcement, collection vs single nav prop |
| `DataModificationItemTests` | `Tests.Core` | New properties, tree flattening/ordering, IsBindOperation |
| `BindReferenceValidatorTests` | `Tests.Core` | Existence validation for bind references, error messages |
| `DeepOperationSettingsTests` | `Tests.Core` | Configuration defaults and validation |
| `EFChangeSetInitializerTests` | `Tests.EntityFramework` + `Tests.AspNetCore` | FK wiring after parent materialization, bind reference resolution |

### Feature Tests (HTTP Integration)

New base classes `DeepInsertTests<TApi, TContext>` and `DeepUpdateTests<TApi, TContext>` in `Tests.AspNetCore/FeatureTests/`, with EF6 and EFCore subclasses.

#### Deep Insert Tests

| Test | Scenario |
|------|----------|
| `DeepInsert_SingleNavProperty` | POST Publisher with inline single Book |
| `DeepInsert_CollectionNavProperty` | POST Publisher with inline Books array |
| `DeepInsert_WithBindReference` | POST Book with `Publisher@odata.bind` |
| `DeepInsert_CollectionWithBind` | POST Publisher with `Books@odata.bind` array |
| `DeepInsert_MixedBindAndCreate` | POST Publisher with some inline Books and some `@odata.bind` |
| `DeepInsert_MultiLevel` | POST Publisher with Books containing Reviews (2-level) |
| `DeepInsert_ExceedsMaxDepth` | Returns 400 when nesting exceeds configured limit |
| `DeepInsert_BindReferenceNotFound` | Returns 400 when `@odata.bind` references non-existent entity |
| `DeepInsert_FiresConventionMethods` | Verifies `OnInsertingBook()` fires for nested Book |

#### Deep Update Tests

| Test | Scenario |
|------|----------|
| `DeepUpdate_Patch_MergeSemantics` | PATCH Publisher with partial Books — existing untouched, new added, matched updated |
| `DeepUpdate_Put_ReplaceSemantics` | PUT Publisher with Books — missing children deleted |
| `DeepUpdate_SingleNavProperty` | PATCH Book with inline Publisher change |
| `DeepUpdate_BindOnUpdate` | PATCH Book with `Publisher@odata.bind` |
| `DeepUpdate_NullUnlinks` | PATCH Book with `Publisher: null` unlinks |
| `DeepUpdate_FiresConventionMethods` | Verifies `OnUpdatingPublisher()` fires for nested update |

All feature tests run on both EF6 and EFCore via the generic base class pattern.

## Files Changed

### New Files

| File | Description |
|------|-------------|
| `src/Microsoft.Restier.Core/Submit/DeepOperationSettings.cs` | Configuration class |
| `src/Microsoft.Restier.Core/Submit/BindReferenceValidator.cs` | @odata.bind existence validator |
| `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationExtractor.cs` | Nested entity extraction |
| `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/Review.cs` | Test entity |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepInsertTests.cs` | Deep insert feature tests |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepUpdateTests.cs` | Deep update feature tests |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EF6/DeepInsertTests.cs` | EF6 subclass |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EF6/DeepUpdateTests.cs` | EF6 subclass |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/DeepInsertTests.cs` | EFCore subclass |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/DeepUpdateTests.cs` | EFCore subclass |
| `test/Microsoft.Restier.Tests.AspNetCore/DeepOperationExtractorTests.cs` | Unit tests |
| `test/Microsoft.Restier.Tests.Core/Submit/DataModificationItemTests.cs` | Unit tests |
| `test/Microsoft.Restier.Tests.Core/Submit/BindReferenceValidatorTests.cs` | Unit tests |

### Modified Files

| File | Change |
|------|--------|
| `src/Microsoft.Restier.Core/Submit/ChangeSetItem.cs` | Add ParentItem, ParentNavigationPropertyName, NestedItems, IsBindOperation to DataModificationItem |
| `src/Microsoft.Restier.Core/Submit/DefaultChangeSetInitializer.cs` | Add protected FK resolution helpers |
| `src/Microsoft.Restier.AspNetCore/RestierController.cs` | Post() and Update() use DeepOperationExtractor, flatten tree into ChangeSet |
| `src/Microsoft.Restier.AspNetCore/Extensions/Extensions.cs` | No functional change — EdmEntityObject skip remains, extraction is handled by DeepOperationExtractor |
| `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs` | Register DeepOperationSettings, add ConfigureDeepOperations builder method |
| `src/Microsoft.Restier.EntityFrameworkCore/Submit/EFChangeSetInitializer.cs` | FK wiring after parent materialization, @odata.bind handling |
| `src/Microsoft.Restier.EntityFramework/Submit/EFChangeSetInitializer.cs` | FK wiring after parent materialization, @odata.bind handling |
| `src/Microsoft.Restier.EntityFramework.Shared/Extensions/ServiceCollectionExtensions.cs` | Register BindReferenceValidator |
| `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/Book.cs` | Add PublisherId FK, Reviews collection |
| `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/Publisher.cs` | No change (already has Books collection) |
| `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryContext.cs` | Add DbSet\<Review\>, configure Review relationship |
| `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryTestInitializer.cs` | Seed Review data |
