// <copyright file="InTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.AspNet.Tests.FeatureTests
{
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Restier.Tests.Shared.AspNet;
    using Microsoft.Restier.Tests.Shared.AspNet.Scenarios.Library;
    using Microsoft.Restier.Tests.Shared.EntityFramework.Scenarios.Library;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// In query tests.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class InTests
    {
        private readonly ITestOutputHelper output;

        /// <summary>
        /// Initializes a new instance of the <see cref="InTests"/> class.
        /// </summary>
        /// <param name="output">The helper to output into during the tests.</param>
        public InTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Tests the id in a list.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task InQueries_IdInList()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Books?$filter=Id in ['c2081e58-21a5-4a15-b0bd-fff03ebadd30','0697576b-d616-4057-9d28-ed359775129e']");
            var content = await response.Content.ReadAsStringAsync();
            this.output.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("Jungle Book, The");
            content.Should().Contain("Color Purple, The");
            content.Should().NotContain("A Clockwork Orange");
        }
    }
}