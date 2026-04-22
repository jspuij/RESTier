---
title: Performance Considerations
description: Performance notes and known limitations for RESTier.
---

# Performance Considerations

## Query Execution and Streaming

RESTier passes `IQueryable` results from Entity Framework through to the OData serializer without buffering the entire result set in memory. For collection queries (e.g., `GET /Products`), the OData serializer enumerates the `IQueryable` directly, which means:

- Results are not fully loaded into memory before serialization begins
- Memory usage is proportional to the serialization buffer, not the full result set
- This is the same pattern used by standard ASP.NET Core OData controllers

For single-entity queries (e.g., `GET /Products(1)`), the result is a single row and is evaluated eagerly in the controller.

## Entity Framework 6: `$expand` and `$select` Materialization

When using **Entity Framework 6** (not EF Core) with `$expand` or `$select` query options, RESTier must materialize the full result set in memory before serialization. This is because OData v9's `SelectExpandBinder` generates LINQ expression trees that contain `IEdmModel` constants, which EF6 cannot translate to SQL.

RESTier works around this by:

1. Stripping the `$expand`/`$select` projection from the LINQ expression tree
2. Adding `Include()` calls for navigation properties referenced by `$expand`
3. Executing the stripped query against EF6 to load entities
4. Re-applying the projection in memory

This workaround does not affect **Entity Framework Core**, which handles these expression trees natively.

If you are using EF6 and working with large result sets combined with `$expand`/`$select`, consider:

- Using server-side paging (`$top` / `$skip`) to limit result sizes
- Migrating to Entity Framework Core, which does not have this limitation
