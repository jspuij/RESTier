// <copyright file="RestierQueryBuilderTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.AspNet.Tests
{
    using System.Data.Entity;
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Restier.Tests.Shared;
    using Microsoft.Restier.Tests.Shared.AspNet;
    using Microsoft.Restier.Tests.Shared.AspNet.Extensions;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Tests for the Restier query builder.
    /// </summary>
    public class RestierQueryBuilderTests
    {
        private readonly ITestOutputHelper output;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierQueryBuilderTests"/> class.
        /// </summary>
        /// <param name="output">The helper to output into during the tests.</param>
        public RestierQueryBuilderTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Tests Int16 as a key.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task TestInt16AsKey()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi, DbContext>(HttpMethod.Get, resource: "/Customers(1)", serviceCollection: this.Di);
            response.IsSuccessStatusCode.Should().BeTrue();
            this.output.WriteLine(await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// Tests Int64 as a key.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task TestInt64AsKey()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi, DbContext>(HttpMethod.Get, resource: "/Stores(1)", serviceCollection: this.Di);
            response.IsSuccessStatusCode.Should().BeTrue();
            this.output.WriteLine(await response.Content.ReadAsStringAsync());
        }

        private void Di(IServiceCollection services)
        {
            services.AddTestStoreApiServices();
        }
    }
}
