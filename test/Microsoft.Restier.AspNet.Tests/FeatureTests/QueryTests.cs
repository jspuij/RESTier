// <copyright file="QueryTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.AspNet.Tests.FeatureTests
{
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Restier.Tests.Shared.AspNet;
    using Microsoft.Restier.Tests.Shared.AspNet.Scenarios.Library;
    using Microsoft.Restier.Tests.Shared.EntityFramework.Scenarios.Library;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Restier tests that cover the general queryablility of the service.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class QueryTests
    {
        private readonly ITestOutputHelper output;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryTests"/> class.
        /// </summary>
        /// <param name="output">The helper to output into during the tests.</param>
        public QueryTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Tests if the query pipeline is correctly returning 200 StatusCodes when EntitySet tables are just empty.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task EmptyEntitySetQueryReturns200Not404()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/LibraryCards");
            var content = await response.Content.ReadAsStringAsync();
            this.output.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        /// <summary>
        /// Tests if the query pipeline is correctly returning 200 StatusCodes when legitimate queries to a resource simply return no results.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task EmptyFilterQueryReturns200Not404()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Books?$filter=Title eq 'Sesame Street'");
            var content = await response.Content.ReadAsStringAsync();
            this.output.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        /// <summary>
        /// Tests if the query pipeline is correctly returning 404 StatusCodes when a resource does not exist.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task NonExistentEntitySetReturns404()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Subscribers");
            var content = await response.Content.ReadAsStringAsync();
            this.output.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Tests if requests to collection navigation properties build as <see cref="ObservableCollection{T}"/> work.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ObservableCollectionsAsCollectionNavigationProperties()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Publishers('Publisher2')/Books");
            var content = await response.Content.ReadAsStringAsync();
            this.output.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}