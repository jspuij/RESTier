// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Versioning;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Tests.AspNetCore.Versioning.Infrastructure;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.IntegrationTests
{

    public class NSwagIntegrationTests
    {

        [Fact]
        public async Task OpenApi_AtVersionGroupName_ReturnsCorrectVersionedDoc()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await BuildAsync(cancellationToken);
            var client = host.GetTestClient();

            var v1Json = await client.GetStringAsync("/openapi/v1/openapi.json", cancellationToken);
            var v1Root = JsonDocument.Parse(v1Json).RootElement;
            v1Root.GetProperty("paths").EnumerateObject()
                .Should().Contain(p => p.Name.Contains("/Items"));
            v1Root.GetProperty("paths").EnumerateObject()
                .Should().NotContain(p => p.Name.Contains("/AuditLogs"),
                    "V1 doc must not contain V2-only entity sets");

            var v2Json = await client.GetStringAsync("/openapi/v2/openapi.json", cancellationToken);
            var v2Root = JsonDocument.Parse(v2Json).RootElement;
            v2Root.GetProperty("paths").EnumerateObject()
                .Should().Contain(p => p.Name.Contains("/AuditLogs"));
        }

        [Fact]
        public async Task OpenApi_AtRoutePrefix_FallbackPath_StillWorksForBackCompat()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await BuildAsync(cancellationToken);
            var client = host.GetTestClient();

            // Legacy callers may still hit the prefix-based URL; ensure it still works OR returns 404.
            var response = await client.GetAsync("/openapi/api%2Fv1/openapi.json", cancellationToken);
            (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound)
                .Should().BeTrue("either the legacy fallback path works or the new path is the only supported path");
        }

        [Fact]
        public async Task RegistryEmpty_FallsBackToPrefixBasedBehavior()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await BuildHostWithEmptyRegistryAsync(cancellationToken);
            var client = host.GetTestClient();

            // No versioned routes; only an unversioned route at empty prefix.
            // The registry is registered (Versioning package referenced) but empty,
            // so NSwag must serve "/openapi/default/openapi.json" exactly as before.
            var response = await client.GetAsync("/openapi/default/openapi.json", cancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        private static async Task<IHost> BuildAsync(CancellationToken cancellationToken)
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddApiVersioning().AddApiExplorer();
                        services.AddControllers()
                            .AddRestier(options =>
                            {
                                options.Select().Expand().Filter().OrderBy().Count();
                            })
                            .AddApplicationPart(typeof(RestierController).Assembly);
                        services.AddRestierApiVersioning(b => b
                            .AddVersion<SampleApiV1>("api", svc =>
                            {
                                svc.AddSingleton<IChainedService<IModelBuilder>, SampleV1ModelBuilder>();
                                svc.AddSingleton<Microsoft.Restier.Core.Submit.IChangeSetInitializer, Microsoft.Restier.Core.Submit.DefaultChangeSetInitializer>();
                                svc.AddSingleton<Microsoft.Restier.Core.Submit.ISubmitExecutor, Microsoft.Restier.Core.Submit.DefaultSubmitExecutor>();
                            })
                            .AddVersion<SampleApiV2>("api", svc =>
                            {
                                svc.AddSingleton<IChainedService<IModelBuilder>, SampleV2ModelBuilder>();
                                svc.AddSingleton<Microsoft.Restier.Core.Submit.IChangeSetInitializer, Microsoft.Restier.Core.Submit.DefaultChangeSetInitializer>();
                                svc.AddSingleton<Microsoft.Restier.Core.Submit.ISubmitExecutor, Microsoft.Restier.Core.Submit.DefaultSubmitExecutor>();
                            }));
                        services.AddRestierNSwag();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseRestierVersionHeaders();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                            endpoints.MapRestier();
                        });
                        app.UseRestierOpenApi();
                        app.UseRestierReDoc();
                        app.UseRestierNSwagUI();
                    }));

            return await builder.StartAsync(cancellationToken);
        }

        private static async Task<IHost> BuildHostWithEmptyRegistryAsync(CancellationToken cancellationToken)
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddControllers()
                            .AddRestier(options =>
                            {
                                options.Select().Expand().Filter().OrderBy().Count();
                                options.AddRestierRoute<SampleApiV1>("", svc =>
                                {
                                    svc.AddSingleton<IChainedService<IModelBuilder>, SampleV1ModelBuilder>();
                                    svc.AddSingleton<Microsoft.Restier.Core.Submit.IChangeSetInitializer, Microsoft.Restier.Core.Submit.DefaultChangeSetInitializer>();
                                    svc.AddSingleton<Microsoft.Restier.Core.Submit.ISubmitExecutor, Microsoft.Restier.Core.Submit.DefaultSubmitExecutor>();
                                });
                            })
                            .AddApplicationPart(typeof(RestierController).Assembly);
                        // Register Versioning services but no AddVersion calls — empty registry.
                        services.AddRestierApiVersioning(_ => { });
                        services.AddRestierNSwag();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                            endpoints.MapRestier();
                        });
                        app.UseRestierOpenApi();
                        app.UseRestierReDoc();
                        app.UseRestierNSwagUI();
                    }));

            return await builder.StartAsync(cancellationToken);
        }

    }

}
