# Deferred Query Materialization

**Date:** 2026-04-22
**Issue:** [OData/RESTier#614](https://github.com/OData/RESTier/issues/614)

## Problem

RESTier's query pipeline materializes the entire result set into memory (via `ToList()` / `ToArrayAsync()`) inside query executors before returning results. This means every query — regardless of size — is fully buffered before the OData serializer sees it. For large entity sets this causes unnecessary memory pressure and prevents streaming serialization.

## Goals

- Let `IQueryable` flow through the pipeline unmaterialized until the OData serializer enumerates it
- Remove the `CheckSubExpressionResult` method that forces early enumeration to detect empty results
- Move single-entity 404 detection from the query handler to the controller, where HTTP semantics belong
- Document the intentional EF6 `SelectExpandHelper` materialization

## Non-Goals

- Changing `QueryResult` or `IQueryExecutor` contracts
- Fixing the EF6 `SelectExpandHelper` materialization (intentional workaround, documented instead)
- Changing submit-path materializations (single-row lookups, negligible)

## Design

### 1. Defer Materialization in `EFQueryExecutor`

**File:** `src/Microsoft.Restier.EntityFramework.Shared/Query/EFQueryExecutor.cs`

Change `ExecuteQueryAsync` to pass the `IQueryable` through without materializing:

```csharp
// Before
return new QueryResult(await query.ToArrayAsync(cancellationToken).ConfigureAwait(false));

// After
return new QueryResult(query);
```

`QueryResult` accepts `IEnumerable`, and `IQueryable` implements `IEnumerable`, so this is a compatible change. The query will be executed when the OData serializer enumerates the results.

The EF6 `SelectExpandHelper` path is unchanged — it must materialize to work around the OData/EF6 expression tree incompatibility.

### 2. Defer Materialization in `DefaultQueryExecutor`

**File:** `src/Microsoft.Restier.Core/Query/DefaultQueryExecutor.cs`

Same change for the fallback (non-EF) executor:

```csharp
// Before
var result = new QueryResult(query.ToList());

// After
var result = new QueryResult(query);
```

The `IQueryable` contract guarantees deferred execution. Custom `IQueryable` sources are expected to handle this.

### 3. Remove `CheckSubExpressionResult` from `DefaultQueryHandler`

**File:** `src/Microsoft.Restier.Core/Query/DefaultQueryHandler.cs`

Remove three methods entirely:
- `CheckSubExpressionResult` — forces enumeration of results just to check emptiness
- `ExecuteSubExpression` — re-executes stripped sub-queries (the 404 check at lines 264-275 is already commented out)
- `CheckWhereCondition` — detects key-predicate Where clauses to throw 404

Remove the call site in `QueryAsync` (lines 127-128):

```csharp
// Remove this block
await CheckSubExpressionResult(
    context, result.Results, visitor, executor, expression, cancellationToken).ConfigureAwait(false);
```

Also remove the three `const string` fields (`ExpressionMethodNameOfWhere`, `ExpressionMethodNameOfSelect`, `ExpressionMethodNameOfSelectMany`) that are only used by the removed methods.

**Why this is safe:** `CheckSubExpressionResult` has two behaviors:
1. Key-predicate 404 detection (via `CheckWhereCondition`) — moved to controller (see section 4)
2. Sub-expression re-execution (via `ExecuteSubExpression`) — the actual 404 throw is commented out, so this currently does nothing useful except waste a database round-trip

### 4. Add 404 Detection to `RestierController`

**File:** `src/Microsoft.Restier.AspNetCore/RestierController.cs`

In `CreateQueryResponse`, the single-entity path (line 527) already calls `query.SingleOrDefault()`. When the result is `null`, the current code returns `204 NoContent`. This needs to differentiate between:

- **Entity by key not found** (`GET /Products(999)`) — should be **404 Not Found**
- **Null-valued property** (`GET /Products(1)/OptionalRelation`) — should be **204 No Content**

Change `CreateQueryResponse` to accept the `ODataPath` and check for key-based requests:

```csharp
var entityResult = query.SingleOrDefault();
if (entityResult is null)
{
    // If the path resolves to a specific entity by key, return 404.
    // Check the last segment (or second-to-last if last is a TypeSegment for type casts
    // like /Products(1)/MyNamespace.SpecialProduct).
    var lastSegment = path.LastOrDefault();
    var isKeyRequest = lastSegment is KeySegment
        || (lastSegment is TypeSegment && path.Count >= 2 && path[path.Count - 2] is KeySegment);

    if (isKeyRequest)
    {
        return NotFound(Resources.ResourceNotFound);
    }

    return NoContent();
}
```

The `ODataPath` is already available at every call site of `CreateQueryResponse` — just thread it through.

This correctly distinguishes:
- `GET /Products(999)` (last segment: `KeySegment`) → **404** when not found
- `GET /Products(1)/MyNamespace.SpecialProduct` (last: `TypeSegment`, prev: `KeySegment`) → **404** when not found
- `GET /Products(1)/Publisher` (last segment: `NavigationPropertySegment`) → **204** when null

### 5. Downstream Auto-Fix: `RestierController.ExecuteQuery`

**File:** `src/Microsoft.Restier.AspNetCore/RestierController.cs`

The `.AsQueryable()` call at line 625:

```csharp
var result = queryResult.Results.AsQueryable();
```

When `Results` holds a live `IQueryable`, `AsQueryable()` returns it unchanged (the extension method checks `is IQueryable<T>` first). No code change needed — this becomes a no-op passthrough automatically.

### 6. Document EF6 `SelectExpandHelper` Materialization

**File:** `docs/msdocs/` (new or existing performance/known-issues page)

Add documentation explaining that when using Entity Framework 6 with `$expand`/`$select`, results are materialized in memory before serialization. This is an intentional workaround for EF6 not being able to translate OData's `SelectExpand` expression trees to SQL. EF Core is not affected.

## Impact on Existing Consumers

| Consumer | Current behavior | After change | Breaking? |
|----------|-----------------|-------------|-----------|
| `RestierController.ExecuteQuery` | `.AsQueryable()` wraps materialized list | `.AsQueryable()` passes through live `IQueryable` | No |
| `RestierController.CreateQueryResponse` | Enumerates in-memory list | Enumerates live `IQueryable` | No |
| `EFChangeSetInitializer` | `.SingleOrDefault()` on materialized list | `.SingleOrDefault()` on live `IQueryable` | No |
| `RestierQueryExecutor` | Delegates to inner, no materialization | Unchanged | No |
| Custom `IQueryExecutor` implementations | N/A — their behavior is their own | N/A | No |

## Testing

- Existing integration tests should continue to pass (query results are the same, just deferred)
- Add/verify tests for 404 on `GET /EntitySet(nonexistent-key)` 
- Add/verify tests for 204 on null single-valued navigation properties
- Verify that `$expand`/`$select` still works on both EF6 and EF Core paths
- Verify `$count` still works (goes through `ExecuteExpressionAsync`, not affected)
