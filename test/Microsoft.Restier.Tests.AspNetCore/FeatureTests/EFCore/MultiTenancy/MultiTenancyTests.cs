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
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EFCore.MultiTenancy;

public class MultiTenancyTests : RestierTestBase<MultiTenantApi>
{
    private static readonly string AcmeDb = $"tenant-acme-{Guid.NewGuid():N}";
    private static readonly string GlobexDb = $"tenant-globex-{Guid.NewGuid():N}";

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
}
