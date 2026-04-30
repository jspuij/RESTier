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

            var defaultResponse = await client.GetAsync("/openapi/default/openapi.json", cancellationToken);
            defaultResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var defaultJson = await defaultResponse.Content.ReadAsStringAsync(cancellationToken);
            JsonDocument.Parse(defaultJson).RootElement.GetProperty("openapi").GetString().Should().StartWith("3.");

            var v3Response = await client.GetAsync("/openapi/v3/openapi.json", cancellationToken);
            v3Response.StatusCode.Should().Be(HttpStatusCode.OK);
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
                                            restierServices.AddSingleton(TestEdmModelBuilder.Build());
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
