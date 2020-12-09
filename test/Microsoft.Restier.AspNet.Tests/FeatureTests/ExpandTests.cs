// <copyright file="ExpandTests.cs" company="Microsoft Corporation">
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
    /// Tests that check expand.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ExpandTests
    {
        private readonly ITestOutputHelper output;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpandTests"/> class.
        /// </summary>
        /// <param name="output">The helper to output into during the tests.</param>
        public ExpandTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Check that a cound combined with expand should not throw an exception.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task CountPlusExpandShouldntThrowExceptions()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Publishers?$expand=Books");
            var content = await response.Content.ReadAsStringAsync();
            this.output.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("A Clockwork Orange");
        }
    }
}