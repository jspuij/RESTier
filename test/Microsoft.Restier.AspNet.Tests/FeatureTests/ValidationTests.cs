// <copyright file="ValidationTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.AspNet.Tests.FeatureTests
{
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Restier.Tests.Shared.AspNet;
    using Microsoft.Restier.Tests.Shared.AspNet.Extensions;
    using Microsoft.Restier.Tests.Shared.AspNet.Scenarios.Library;
    using Microsoft.Restier.Tests.Shared.EntityFramework.Scenarios.Library;
    using Microsoft.Restier.Tests.Shared.Scenarios.Library;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Entity validation tests.
    /// </summary>
    public class ValidationTests
    {
        private readonly ITestOutputHelper output;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationTests"/> class.
        /// </summary>
        /// <param name="output">The helper to output into during the tests.</param>
        public ValidationTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Tests for validation of an entity, in this case that a string length is too long on an entity.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task Validation_StringLengthExceeded()
        {
            var bookRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Books?$top=1", acceptHeader: ODataConstants.DefaultAcceptHeader);
            bookRequest.IsSuccessStatusCode.Should().BeTrue();
            var (bookList, errorContent) = await bookRequest.DeserializeResponseAsync<ODataV4List<Book>>();

            bookList.Should().NotBeNull();
            bookList.Items.Should().NotBeEmpty();
            var book = bookList.Items.First();

            book.Should().NotBeNull();

            book.Isbn = "This is a really really long string.";

            var bookEditRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Put, resource: $"/Books({book.Id})", payload: book, acceptHeader: WebApiConstants.DefaultAcceptHeader);
            bookEditRequest.IsSuccessStatusCode.Should().BeFalse();
            var content = await bookEditRequest.Content.ReadAsStringAsync();
            this.output.WriteLine(content);
            content.Should().Contain("validationentries");
            content.Should().Contain("MaxLengthAttribute");
        }
    }
}