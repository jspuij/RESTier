# Getting Started

This guide walks you through creating a simple OData V4 API using RESTier with ASP.NET Core and Entity Framework Core. By the end, you will have a working bookstore API that supports querying, filtering, sorting, and CRUD operations out of the box.

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) or later

## 1. Create a New Project

Create a new ASP.NET Core Web API project:

```bash
dotnet new web -n BookstoreApi
cd BookstoreApi
```

## 2. Install NuGet Packages

Add the RESTier packages and an Entity Framework Core database provider:

```bash
dotnet add package Microsoft.Restier.AspNetCore
dotnet add package Microsoft.Restier.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.InMemory
```

> **Tip:** For a real application, replace `Microsoft.EntityFrameworkCore.InMemory` with a production provider such as `Microsoft.EntityFrameworkCore.SqlServer` or `Npgsql.EntityFrameworkCore.PostgreSQL`.

## 3. Define the Entity Model

Create a `Book.cs` file with a simple entity class:

```csharp
namespace BookstoreApi;

public class Book
{
    public int Id { get; set; }

    public string Title { get; set; }

    public string Author { get; set; }

    public decimal Price { get; set; }

    public int Year { get; set; }
}
```

## 4. Create the DbContext

Create a `BookstoreContext.cs` file. The `DbSet` properties you define here become OData EntitySets automatically:

```csharp
using Microsoft.EntityFrameworkCore;

namespace BookstoreApi;

public class BookstoreContext : DbContext
{
    public BookstoreContext(DbContextOptions<BookstoreContext> options)
        : base(options)
    {
    }

    public DbSet<Book> Books { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Seed some sample data
        modelBuilder.Entity<Book>().HasData(
            new Book { Id = 1, Title = "Clean Code", Author = "Robert C. Martin", Price = 31.99m, Year = 2008 },
            new Book { Id = 2, Title = "The Pragmatic Programmer", Author = "David Thomas", Price = 49.99m, Year = 2019 },
            new Book { Id = 3, Title = "Design Patterns", Author = "Erich Gamma", Price = 39.99m, Year = 1994 }
        );
    }
}
```

## 5. Create the RESTier API Class

Create a `BookstoreApi.cs` file. This class connects RESTier to your DbContext. All dependencies are provided through constructor injection:

```csharp
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFrameworkCore;

namespace BookstoreApi;

public class BookstoreApi : EntityFrameworkApi<BookstoreContext>
{
    public BookstoreApi(
        BookstoreContext dbContext,
        IEdmModel model,
        IQueryHandler queryHandler,
        ISubmitHandler submitHandler)
        : base(dbContext, model, queryHandler, submitHandler)
    {
    }
}
```

RESTier automatically exposes every `DbSet` on your context as a queryable OData EntitySet. No controller code is needed.

## 6. Configure Services in Program.cs

Replace the contents of `Program.cs` with the following:

```csharp
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.EntityFrameworkCore;
using BookstoreApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddRestier(options =>
    {
        // Enable standard OData query options
        options.Select().Expand().Filter().OrderBy().SetMaxTop(100).Count();

        // Register the RESTier API with a route prefix
        options.AddRestierRoute<BookstoreApi.BookstoreApi>("api", routeServices =>
        {
            routeServices.AddEFCoreProviderServices<BookstoreContext>(dbOptions =>
                dbOptions.UseInMemoryDatabase("Bookstore"));
        });
    });

var app = builder.Build();

// Ensure the database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
    db.Database.EnsureCreated();
}

app.UseRouting();
app.MapControllers();
app.MapRestier();

app.Run();
```

Key points about the configuration:

- **`AddRestier`** registers RESTier and OData services. The lambda configures which OData query options are enabled.
- **`AddRestierRoute<TApi>`** maps your API class to a route prefix (`"api"` in this example). Use an empty string for no prefix.
- **`AddEFCoreProviderServices<TDbContext>`** registers Entity Framework Core as the data provider and configures the DbContext.
- **`MapRestier()`** sets up the dynamic routing that dispatches OData requests to the RESTier controller.

## 7. Run the Application

Start the application:

```bash
dotnet run
```

The API is now available. Try the following URLs in a browser or with `curl` (assuming the default port):

| URL | Description |
|-----|-------------|
| `http://localhost:5000/api` | OData service document listing available EntitySets |
| `http://localhost:5000/api/$metadata` | OData metadata document (CSDL) describing the entity model |
| `http://localhost:5000/api/Books` | Query all books |
| `http://localhost:5000/api/Books(1)` | Get a single book by key |
| `http://localhost:5000/api/Books?$filter=Price lt 40` | Filter books where Price is less than 40 |
| `http://localhost:5000/api/Books?$select=Title,Author` | Return only the Title and Author properties |
| `http://localhost:5000/api/Books?$orderby=Year desc` | Sort books by Year in descending order |
| `http://localhost:5000/api/Books?$top=2&$skip=1` | Pagination: skip the first result and take two |
| `http://localhost:5000/api/Books/$count` | Return the total count of books |

RESTier also supports full CRUD operations. You can create, update, and delete books by sending `POST`, `PATCH`/`PUT`, and `DELETE` requests to the appropriate URLs.

## Next Steps

Now that you have a working RESTier API, explore these topics to add more capabilities:

- **[EntitySet Filters](server/filters.md)** -- Automatically filter query results based on business rules or the current user.
- **[Method Authorization](server/method-authorization.md)** -- Control which CRUD operations are allowed on each EntitySet.
- **[Interceptors](server/interceptors.md)** -- Run custom logic before and after entities are inserted, updated, or deleted.
- **[Customizing the Entity Model](server/model-building.md)** -- Adjust the OData model that RESTier generates from your DbContext.
- **[Naming Conventions](server/naming-conventions.md)** -- Use camelCase property names in JSON payloads for JavaScript-friendly APIs.
- **[Optimistic Concurrency](server/concurrency.md)** -- Use ETags to prevent lost updates with `If-Match` and `If-None-Match` headers.
- **[Operations](server/operations.md)** -- Add custom OData actions and functions to your API.
- **[OpenAPI / Swagger](server/swagger.md)** -- Generate interactive API documentation.
- **[Testing with Breakdance](server/testing.md)** -- Write in-memory integration tests for your API.
- **[Temporal Types](extending-restier/temporal-types.md)** -- Work with date and time types in your OData model.
- **[In-Memory Provider](extending-restier/in-memory-provider.md)** -- Use a non-EF data source with RESTier.
