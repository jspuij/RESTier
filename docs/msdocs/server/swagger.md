# OpenAPI / Swagger Support

RESTier can automatically generate an [OpenAPI](https://www.openapis.org/) (formerly Swagger) document from
your EDM model and serve an interactive Swagger UI for exploring your API. This is provided by the
`Microsoft.Restier.AspNetCore.Swagger` package, which builds on
[Microsoft.OpenApi.OData](https://github.com/microsoft/OpenAPI.NET.OData) for document generation and
[Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore) for the UI.

## Setup

### Install the NuGet Package

```bash
dotnet add package Microsoft.Restier.AspNetCore.Swagger
```

### Register Services

In your `Program.cs`, call `AddRestierSwagger()` on the service collection:

```csharp
builder.Services.AddRestierSwagger();
```

### Add Middleware

After building the app but before `app.Run()`, call `UseRestierSwaggerUI()`:

```csharp
app.UseRestierSwaggerUI();
```

### Complete Example

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRestierSwagger();

builder.Services
    .AddRestier((restierBuilder) =>
    {
        restierBuilder.AddRestierApi<MyApi>(services =>
        {
            // configure your API services here
        });
    })
    .AddOData(options =>
    {
        options.AddRouteComponents("api", builder => builder.AddRestierModel<MyApi>());
    });

var app = builder.Build();

app.UseRestierSwaggerUI();

app.MapRestier(builder =>
{
    builder.MapApiRoute<MyApi>("api");
});

app.Run();
```

## Usage

Once the middleware is registered, two endpoints become available:

| Endpoint | Description |
|----------|-------------|
| `/swagger` | Interactive Swagger UI for browsing and testing your API |
| `/swagger/{documentName}/swagger.json` | Raw OpenAPI 3.0 JSON document |

The `{documentName}` corresponds to the OData route prefix you registered. If you registered a route with
the prefix `"api"`, the document URL will be `/swagger/api/swagger.json`. If the route prefix is empty,
the document name defaults to `"default"`, so the URL will be `/swagger/default/swagger.json`.

## Configuration

You can customize the generated OpenAPI document by passing an `Action<OpenApiConvertSettings>` to
`AddRestierSwagger()`. The `OpenApiConvertSettings` class comes from the
[Microsoft.OpenApi.OData](https://github.com/microsoft/OpenAPI.NET.OData) package and controls how the
EDM model is converted to OpenAPI.

```csharp
builder.Services.AddRestierSwagger(settings =>
{
    settings.TopExample = 10;
    settings.PathPrefix = "v1";
    settings.EnableKeyAsSegment = true;
});
```

> **Note:** RESTier automatically sets `TopExample` to your configured `MaxTop` value from
> `ODataValidationSettings` and populates `ServiceRoot` from the incoming HTTP request. Any values you
> set in the configuration action will override these defaults.

For the full list of available settings, refer to the
[OpenApiConvertSettings documentation](https://github.com/microsoft/OpenAPI.NET.OData#readme).

## Multiple APIs

If your application registers multiple Restier APIs with different route prefixes, `UseRestierSwaggerUI()`
automatically discovers all of them and creates a separate OpenAPI document for each. The Swagger UI will
show a dropdown in the top-right corner that lets you switch between APIs.

For example, if you register two routes:

```csharp
builder.Services
    .AddRestier((restierBuilder) =>
    {
        restierBuilder.AddRestierApi<TripsApi>(services => { /* ... */ });
        restierBuilder.AddRestierApi<BookingsApi>(services => { /* ... */ });
    })
    .AddOData(options =>
    {
        options.AddRouteComponents("trips", builder => builder.AddRestierModel<TripsApi>());
        options.AddRouteComponents("bookings", builder => builder.AddRestierModel<BookingsApi>());
    });
```

Two OpenAPI documents will be served:

- `/swagger/trips/swagger.json`
- `/swagger/bookings/swagger.json`

Both will appear in the Swagger UI dropdown at `/swagger`.
