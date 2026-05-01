// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Tests.AspNetCore.NSwag.Infrastructure;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.NSwag.Extensions
{

    public class IApplicationBuilderExtensionsTests
    {

        [Fact]
        public async Task UseRestierOpenApi_ServesEachRegisteredRouteUnderItsName()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await BuildHostAsync(routes: new[] { ("", typeof(TestApi)), ("v3", typeof(TestApi)) }, cancellationToken);
            var client = host.GetTestClient();

            foreach (var (urlPath, expectedServerSuffix) in new[]
            {
                ("/openapi/default/openapi.json", string.Empty),
                ("/openapi/v3/openapi.json", "/v3"),
            })
            {
                var response = await client.GetAsync(urlPath, cancellationToken);
                response.StatusCode.Should().Be(HttpStatusCode.OK, $"path {urlPath} must serve OpenAPI");
                response.Content.Headers.ContentType?.MediaType.Should().Be("application/json", $"path {urlPath} must declare JSON content type");

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                root.GetProperty("openapi").GetString().Should().StartWith("3.", $"path {urlPath} must serve OpenAPI 3.x");
                root.GetProperty("paths").EnumerateObject()
                    .Should().Contain(p => p.Name.Contains("/Items", StringComparison.OrdinalIgnoreCase),
                        $"path {urlPath} must include the Items entity-set paths discovered from TestApi");

                var serverUrl = root.GetProperty("servers")[0].GetProperty("url").GetString();
                serverUrl.Should().EndWith(expectedServerSuffix, $"path {urlPath} server URL must reflect the route prefix");
            }
        }

        [Fact]
        public async Task UseRestierOpenApi_ReturnsNotFound_ForUnknownDocumentName()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await BuildHostAsync(routes: new[] { ("", typeof(TestApi)) }, cancellationToken);
            var client = host.GetTestClient();

            var response = await client.GetAsync("/openapi/nonexistent/openapi.json", cancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        private static async Task<IHost> BuildHostAsync((string prefix, Type apiType)[] routes, CancellationToken cancellationToken)
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services
                            .AddControllers()
                            .AddRestier(options =>
                            {
                                foreach (var (prefix, apiType) in routes)
                                {
                                    if (apiType == typeof(TestApi))
                                    {
                                        options.AddRestierRoute<TestApi>(prefix, restierServices =>
                                        {
                                            restierServices.AddSingleton<IChainedService<IModelBuilder>, TestApiModelBuilder>();
                                        });
                                    }
                                }
                            });
                        services.AddRestierNSwag();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints => endpoints.MapRestier());
                        app.UseRestierOpenApi();
                    }));

            return await builder.StartAsync(cancellationToken);
        }

    }

}
