// <copyright file="FunctionTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.AspNet.Tests.FeatureTests
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Restier.Tests.Shared.AspNet;
    using Microsoft.Restier.Tests.Shared.AspNet.Scenarios.Library;
    using Microsoft.Restier.Tests.Shared.EntityFramework.Scenarios.Library;
    using Microsoft.Restier.Tests.Shared.Scenarios.Library;
    using Newtonsoft.Json;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Function call tests.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class FunctionTests
    {
        private readonly ITestOutputHelper output;

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionTests"/> class.
        /// </summary>
        /// <param name="output">The helper to output into during the tests.</param>
        public FunctionTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Tests the function with filter.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task FunctionWithFilter()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/FavoriteBooks()?$filter=contains(Title,'Cat')");
            var content = await response.Content.ReadAsStringAsync();
            this.output.WriteLine(content);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("Cat");
            content.Should().NotContain("Mouse");
        }

        /// <summary>
        /// Tests the function with expand.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task FunctionWithExpand()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/FavoriteBooks()?$expand=Publisher");
            var content = await response.Content.ReadAsStringAsync();
            this.output.WriteLine(content);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("Publisher Way");
        }

        /// <summary>
        /// Tests a bound function with expand.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task BoundFunctionWithExpand()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Publishers/Publisher1/PublishedBooks()?$expand=Publisher");
            var content = await response.Content.ReadAsStringAsync();
            this.output.WriteLine(content);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("Publisher Way");
        }

        /// <summary>
        /// Test calling a function with a boolean parameter.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task FunctionParameters_BooleanParameter()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/PublishBook(IsActive=true)");
            var content = await response.Content.ReadAsStringAsync();
            this.output.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("in the Hat");
        }

        /// <summary>
        /// Test calling a function with an int parameter.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task FunctionParameters_IntParameter()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/PublishBooks(Count=5)");
            var content = await response.Content.ReadAsStringAsync();
            this.output.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("Comes Back");
        }

        /// <summary>
        /// Tests calling a function with a guid parameter.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task FunctionParameters_GuidParameter()
        {
            var testGuid = Guid.NewGuid();
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: $"/SubmitTransaction(Id={testGuid})");
            var content = await response.Content.ReadAsStringAsync();
            this.output.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain(testGuid.ToString());
            content.Should().Contain("Shrugged");
        }

        /// <summary>
        /// Tests if the query pipeline is correctly returning 200 StatusCodes when legitimate queries to a resource simply return no results.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact(Skip = "Seems to fail in Odata handler itself.")]
        public async Task BoundFunctions_CanHaveFilterPathSegment()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Books/$filter(endswith(Title,'The'))/DiscontinueBooks()");
            var content = await response.Content.ReadAsStringAsync();
            this.output.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var results = JsonConvert.DeserializeObject<ODataV4List<Book>>(content);
            results.Should().NotBeNull();
            results.Items.Should().NotBeEmpty();
            results.Items.Should().HaveCount(2);
            results.Items.Should().OnlyContain(b => b.Title.EndsWith(" | Discontinued", StringComparison.CurrentCulture));
        }

        /// <summary>
        /// Tests if the query pipeline is correctly returning 200 StatusCodes when legitimate queries to a resource simply return no results.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task BoundFunctions_Returns200()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Books/DiscontinueBooks()");
            var content = await response.Content.ReadAsStringAsync();
            this.output.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var results = JsonConvert.DeserializeObject<ODataV4List<Book>>(content);
            results.Should().NotBeNull();
            results.Items.Should().NotBeEmpty();
            results.Items.Count.Should().BeOneOf(3, 5);
            results.Items.Should().OnlyContain(b => b.Title.EndsWith(" | Intercepted | Discontinued | Intercepted", StringComparison.CurrentCulture));
        }
    }
}