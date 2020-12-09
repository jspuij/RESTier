// <copyright file="ActionTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.AspNet.Tests
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Restier.Tests.Shared.AspNet;
    using Microsoft.Restier.Tests.Shared.AspNet.Scenarios.Library;
    using Microsoft.Restier.Tests.Shared.EntityFramework.Scenarios.Library;
    using Microsoft.Restier.Tests.Shared.Scenarios.Library;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// OData Action tests.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ActionTests
    {
        private readonly ITestOutputHelper output;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionTests"/> class.
        /// </summary>
        /// <param name="output">The helper to output into during the tests.</param>
        public ActionTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Missing parameter tests.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ActionParameters_MissingParameter()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Post, resource: "/CheckoutBook");
            var content = await response.Content.ReadAsStringAsync();
            this.output.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeFalse();
            content.Should().Contain("NullReferenceException");
        }

        /// <summary>
        /// Tests for wrong parameter name.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ActionParameters_WrongParameterName()
        {
            var bookPayload = new
            {
                john = new Book
                {
                    Id = Guid.NewGuid(),
                    Title = "Constantly Frustrated: the Robert McLaws Story",
                },
            };

            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Post, resource: "/CheckoutBook", acceptHeader: WebApiConstants.DefaultAcceptHeader, payload: bookPayload);
            var content = await response.Content.ReadAsStringAsync();
            this.output.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeFalse();
            content.Should().Contain("Model state is not valid");
        }

        /// <summary>
        /// Tests for Has Parameter.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ActionParameters_HasParameter()
        {
            var bookPayload = new
            {
                book = new Book
                {
                    Id = Guid.NewGuid(),
                    Title = "Constantly Frustrated: the Robert McLaws Story",
                },
            };

            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Post, resource: "/CheckoutBook", acceptHeader: WebApiConstants.DefaultAcceptHeader, payload: bookPayload);
            var content = await response.Content.ReadAsStringAsync();
            this.output.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("Robert McLaws");
            content.Should().Contain("| Submitted");
        }
    }
}