// <copyright file="Issue541_CountPlusParametersFails.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.AspNet.Tests.RegressionTests
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Restier.Tests.Shared.AspNet;
    using Microsoft.Restier.Tests.Shared.AspNet.Extensions;
    using Microsoft.Restier.Tests.Shared.AspNet.Scenarios.Library;
    using Microsoft.Restier.Tests.Shared.EntityFramework.Scenarios.Library;
    using Xunit;

    /// <summary>
    /// Regression tests for https://github.com/OData/RESTier/issues/541.
    /// </summary>
    public class Issue541_CountPlusParametersFails
    {
        /// <summary>
        /// Count should not throw an exception.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CountShouldntThrowExceptions()
        {
            var client = await RestierTestHelpers.GetTestableHttpClient<LibraryApi, LibraryContext>();
            var response = await client.ExecuteTestRequest(HttpMethod.Get, resource: "/Readers?$count=true");
            var content = await response.Content.ReadAsStringAsync();

            content.Should().Contain("\"@odata.count\":2,");
        }

        /// <summary>
        /// Count plus top should not throw exceptions.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CountPlusTopShouldntThrowExceptions()
        {
            var client = await RestierTestHelpers.GetTestableHttpClient<LibraryApi, LibraryContext>();
            var response = await client.ExecuteTestRequest(HttpMethod.Get, resource: "/Readers?$top=5&$count=true");
            var content = await response.Content.ReadAsStringAsync();

            content.Should().Contain("\"@odata.count\":2,");
        }

        /// <summary>
        /// Count plus top plus filter should not throw exceptions.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CountPlusTopPlusFilterShouldntThrowExceptions()
        {
            var client = await RestierTestHelpers.GetTestableHttpClient<LibraryApi, LibraryContext>();
            var response = await client.ExecuteTestRequest(HttpMethod.Get, resource: "/Readers?$top=5&$count=true&$filter=FullName eq 'p1'");
            var content = await response.Content.ReadAsStringAsync();

            content.Should().Contain("\"@odata.count\":1,");
        }

        /// <summary>
        /// Count plus top plus filter.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CountPlusTopPlusProjectionShouldntThrowExceptions()
        {
            var client = await RestierTestHelpers.GetTestableHttpClient<LibraryApi, LibraryContext>();
            var response = await client.ExecuteTestRequest(HttpMethod.Get, resource: "/Readers?$top=5&$count=true&$select=Id,FullName");
            var content = await response.Content.ReadAsStringAsync();

            content.Should().Contain("\"@odata.count\":2,");
        }

        /// <summary>
        /// Count plus select should not throw exceptions.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CountPlusSelectShouldntThrowExceptions()
        {
            var client = await RestierTestHelpers.GetTestableHttpClient<LibraryApi, LibraryContext>();
            var response = await client.ExecuteTestRequest(HttpMethod.Get, resource: "/Readers?$count=true&$select=Id,FullName");
            var content = await response.Content.ReadAsStringAsync();

            content.Should().Contain("\"@odata.count\":2,");
        }

        /// <summary>
        /// Count plus expand should not throw exceptions.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CountPlusExpandShouldntThrowExceptions()
        {
            var client = await RestierTestHelpers.GetTestableHttpClient<LibraryApi, LibraryContext>();
            var response = await client.ExecuteTestRequest(HttpMethod.Get, resource: "/Publishers?$top=5&$count=true&$expand=Books");
            var content = await response.Content.ReadAsStringAsync();

            content.Should().Contain("\"@odata.count\":2,");
        }
    }
}