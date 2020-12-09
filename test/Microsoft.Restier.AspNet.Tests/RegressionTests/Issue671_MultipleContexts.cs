// <copyright file="Issue671_MultipleContexts.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.AspNet.Tests.RegressionTests
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using FluentAssertions;
    using Microsoft.AspNet.OData.Extensions;
    using Microsoft.AspNet.OData.Query;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Restier.EntityFramework;
    using Microsoft.Restier.Tests.Shared.AspNet;
    using Microsoft.Restier.Tests.Shared.AspNet.Extensions;
    using Microsoft.Restier.Tests.Shared.AspNet.Scenarios.Library;
    using Microsoft.Restier.Tests.Shared.AspNet.Scenarios.Marvel;
    using Microsoft.Restier.Tests.Shared.EntityFramework.Scenarios.Library;
    using Microsoft.Restier.Tests.Shared.EntityFramework.Scenarios.Marvel;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Regression tests for https://github.com/OData/RESTier/issues/671.
    /// </summary>
    public class Issue671_MultipleContexts
    {
        private static readonly DefaultQuerySettings QueryDefaults = new DefaultQuerySettings
        {
            EnableCount = true,
            EnableExpand = true,
            EnableFilter = true,
            EnableOrderBy = true,
            EnableSelect = true,
            MaxTop = 10,
        };

        private readonly ITestOutputHelper output;

        /// <summary>
        /// Initializes a new instance of the <see cref="Issue671_MultipleContexts"/> class.
        /// </summary>
        /// <param name="output">The helper to output into during the tests.</param>
        public Issue671_MultipleContexts(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Tests if the query pipeline is correctly returning 200 StatusCodes when EntitySet tables are just empty.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task SingleContext_LibraryApiWorks()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/LibraryCards");
            var content = await response.Content.ReadAsStringAsync();
            this.output.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        /// <summary>
        /// Tests if the query pipeline is correctly returning 200 StatusCodes when EntitySet tables are just empty.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task SingleContext_MarvelApiWorks()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<MarvelApi, MarvelContext>(HttpMethod.Get, resource: "/Characters");
            var content = await response.Content.ReadAsStringAsync();
            this.output.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        /// <summary>
        /// Multiple contexts should query the first context.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task MultipleContexts_ShouldQueryFirstContext()
        {
            var config = new HttpConfiguration();
            Action<IServiceCollection> libraryServices = (services) =>
            {
                services.AddEF6ProviderServices<LibraryContext>();
            };
            Action<IServiceCollection> marvelServices = (services) =>
            {
                services.AddEF6ProviderServices<MarvelContext>();
            };

            config.SetDefaultQuerySettings(QueryDefaults);
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            config.SetTimeZoneInfo(TimeZoneInfo.Utc);

            config.UseRestier<LibraryApi>(libraryServices);
            config.UseRestier<MarvelApi>(marvelServices);
            config.MapRestier<LibraryApi>("Library", "Library", false);
            config.MapRestier<MarvelApi>("Marvel", "Marvel", false);

            var client = config.GetTestableHttpClient();
            var response = await client.ExecuteTestRequest(HttpMethod.Get, routePrefix: "Library", resource: "/Books");

            var content = await response.Content.ReadAsStringAsync();
            this.output.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("\"@odata.count\":3,");
        }

        /// <summary>
        /// Multiple contexts should query the second context.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task MultipleContexts_ShouldQuerySecondContext()
        {
            var config = new HttpConfiguration();
            Action<IServiceCollection> libraryServices = (services) =>
            {
                services.AddEF6ProviderServices<LibraryContext>();
            };
            Action<IServiceCollection> marvelServices = (services) =>
            {
                services.AddEF6ProviderServices<MarvelContext>();
            };

            config.SetDefaultQuerySettings(QueryDefaults);
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            config.SetTimeZoneInfo(TimeZoneInfo.Utc);

            config.UseRestier<LibraryApi>(libraryServices);
            config.UseRestier<MarvelApi>(marvelServices);
            config.MapRestier<LibraryApi>("Library", "Library", false);
            config.MapRestier<MarvelApi>("Marvel", "Marvel", false);

            var client = config.GetTestableHttpClient();
            var response = await client.ExecuteTestRequest(HttpMethod.Get, routePrefix: "Marvel", resource: "/Characters?$count=true");

            var content = await response.Content.ReadAsStringAsync();
            this.output.WriteLine(content);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("\"@odata.count\":1,");
        }
    }
}
