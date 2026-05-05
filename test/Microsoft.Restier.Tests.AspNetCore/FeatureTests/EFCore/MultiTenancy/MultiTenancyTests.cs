// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.EntityFrameworkCore;
using CloudNimble.Breakdance.AspNetCore;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EFCore.MultiTenancy;

public class MultiTenancyTests : RestierTestBase<MultiTenantApi>
{
    private static readonly string AcmeDb = $"tenant-acme-{Guid.NewGuid():N}";
    private static readonly string GlobexDb = $"tenant-globex-{Guid.NewGuid():N}";

    // TenantToDb is captured by value when the InMemoryTenantConnectionStringProvider
    // is constructed in the test-class constructor. Adding entries here AFTER the
    // constructor runs has no effect on the provider's lookup table. All tenants
    // must be declared in this dictionary before any test method runs.
    private static readonly Dictionary<string, string> TenantToDb = new(StringComparer.OrdinalIgnoreCase)
    {
        ["acme"] = AcmeDb,
        ["globex"] = GlobexDb,
    };

    public MultiTenancyTests()
    {
        // App-level services: the middleware reads ITenantContext from the app scope.
        TestHostBuilder.ConfigureServices((_, services) =>
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ITenantContext, TenantContext>();
            services.AddSingleton<IConnectionStringProvider>(
                new InMemoryTenantConnectionStringProvider(TenantToDb));
        });

        // Pipeline middleware: must run BEFORE UseRouting. RestierBreakdanceTestBase
        // invokes ApplicationBuilderAction first in its pipeline (before UseRouting).
        ApplicationBuilderAction = builder =>
        {
            builder.UseMiddleware<PathSegmentTenantResolutionMiddleware>();
        };

        // Route-level services: registered at the OData prefix "odata".
        AddRestierAction = options =>
        {
            options.AddRestierRoute<MultiTenantApi>("odata", services =>
            {
                services.AddHttpContextAccessor();
                services.AddSingleton<IConnectionStringProvider>(
                    new InMemoryTenantConnectionStringProvider(TenantToDb));

                services.AddEFCoreProviderServices<TenantDbContext>((sp, opt) =>
                {
                    // The lambda runs TWICE: once at model-build time (RESTier instantiates
                    // TenantDbContext to inspect its DbSets for EDM construction; HttpContext
                    // is null at that point) and once per request. The placeholder DB name
                    // is only ever used for EDM reflection — RESTier never opens it.
                    //
                    // Note we resolve ITenantContext via http.RequestServices, NOT via sp.
                    // sp is the route-level scoped provider; ITenantContext is registered
                    // in app-level DI (so the middleware can populate it before routing).
                    // sp.GetRequiredService<ITenantContext>() would throw — the route
                    // container has no such registration. The bridge is intentional.
                    var http = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
                    var dbName = http != null
                        ? sp.GetRequiredService<IConnectionStringProvider>()
                              .GetConnectionString(
                                  http.RequestServices.GetRequiredService<ITenantContext>().TenantId
                                  ?? "__model_build__")
                        : "__model_build__";
                    opt.UseInMemoryDatabase(dbName);
                });
            });
        };

        TestSetup();

        SeedTenant(AcmeDb, "AcmeBook");
        SeedTenant(GlobexDb, "GlobexBook");
    }

    private static void SeedTenant(string dbName, string title)
    {
        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        using var ctx = new TenantDbContext(opts);
        ctx.Books.Add(new Book { Id = Guid.NewGuid(), Title = title });
        ctx.SaveChanges();
    }

    [Fact]
    public async Task Acme_GetsAcmeData()
    {
        var response = await ExecuteTestRequest(
            HttpMethod.Get,
            routePrefix: "acme/odata",
            resource: "/Books");
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("AcmeBook");
        content.Should().NotContain("GlobexBook");
    }

    [Fact]
    public async Task Globex_GetsGlobexData()
    {
        var response = await ExecuteTestRequest(
            HttpMethod.Get,
            routePrefix: "globex/odata",
            resource: "/Books");
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("GlobexBook");
        content.Should().NotContain("AcmeBook");
    }

    [Fact]
    public async Task CrossTenantIsolation_PostToAcme_DoesNotLeakToGlobex()
    {
        var newBookTitle = $"NewAcmeBook-{Guid.NewGuid():N}";
        var postResponse = await ExecuteTestRequest(
            HttpMethod.Post,
            routePrefix: "acme/odata",
            resource: "/Books",
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            payload: new { Id = Guid.NewGuid(), Title = newBookTitle });
        _ = await TraceListener.LogAndReturnMessageContentAsync(postResponse);
        postResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

        var getGlobex = await ExecuteTestRequest(
            HttpMethod.Get,
            routePrefix: "globex/odata",
            resource: "/Books");
        var globexContent = await TraceListener.LogAndReturnMessageContentAsync(getGlobex);

        getGlobex.StatusCode.Should().Be(HttpStatusCode.OK);
        globexContent.Should().NotContain(newBookTitle, because: "the new book was POSTed to acme; it must not be visible to globex");
    }

    [Fact]
    public async Task OdataContextUrlPreservesTenantPrefix()
    {
        var response = await ExecuteTestRequest(
            HttpMethod.Get,
            routePrefix: "acme/odata",
            resource: "/Books");
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("/acme/odata/$metadata#Books",
            because: "if PathBase is preserved, generated context URLs include the tenant segment so OData clients can follow links back");
    }

    [Fact]
    public async Task UnknownTenant_Returns400()
    {
        var response = await ExecuteTestRequest(
            HttpMethod.Get,
            routePrefix: "unknown/odata",
            resource: "/Books");
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
