// <copyright file="UpdateTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.AspNet.Tests.FeatureTests
{
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Restier.Tests.Shared.AspNet;
    using Microsoft.Restier.Tests.Shared.AspNet.Extensions;
    using Microsoft.Restier.Tests.Shared.AspNet.Scenarios.Library;
    using Microsoft.Restier.Tests.Shared.EntityFramework.Scenarios.Library;
    using Microsoft.Restier.Tests.Shared.Scenarios.Library;
    using Xunit;

    /// <summary>
    /// Update tests.
    /// </summary>
    public class UpdateTests
    {
        /// <summary>
        /// Updating a book with a publisher included should return 400.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task UpdateBookWithPublisher_ShouldReturn400()
        {
            var bookRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Books?$filter=Id eq 19d68c75-1313-4369-b2bf-521f2b260a59&$expand=Publisher", acceptHeader: ODataConstants.DefaultAcceptHeader);
            bookRequest.IsSuccessStatusCode.Should().BeTrue();
            var (bookList, errorContent) = await bookRequest.DeserializeResponseAsync<ODataV4List<Book>>();

            bookList.Should().NotBeNull();
            bookList.Items.Should().NotBeEmpty();
            var book = bookList.Items.First();

            book.Should().NotBeNull();
            book.Publisher.Should().NotBeNull();

            book.Title += " Test";

            var bookEditRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Put, resource: $"/Books({book.Id})", payload: book, acceptHeader: WebApiConstants.DefaultAcceptHeader);
            bookEditRequest.IsSuccessStatusCode.Should().BeFalse();
            bookEditRequest.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Tests updating a book.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task UpdateBook()
        {
            var bookRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Books?$top=1", acceptHeader: ODataConstants.DefaultAcceptHeader);
            bookRequest.IsSuccessStatusCode.Should().BeTrue();
            var (bookList, errorContent) = await bookRequest.DeserializeResponseAsync<ODataV4List<Book>>();

            bookList.Should().NotBeNull();
            bookList.Items.Should().NotBeEmpty();
            var book = bookList.Items.First();

            book.Should().NotBeNull();

            book.Title += " Test";

            var bookEditRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Put, resource: $"/Books({book.Id})", payload: book, acceptHeader: WebApiConstants.DefaultAcceptHeader);
            bookEditRequest.IsSuccessStatusCode.Should().BeTrue();
        }

        /// <summary>
        /// Tests patching a book.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task PatchBook()
        {
            var bookRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Books?$top=1", acceptHeader: ODataConstants.DefaultAcceptHeader);
            bookRequest.IsSuccessStatusCode.Should().BeTrue();
            var (bookList, errorContent) = await bookRequest.DeserializeResponseAsync<ODataV4List<Book>>();

            bookList.Should().NotBeNull();
            bookList.Items.Should().NotBeEmpty();
            var book = bookList.Items.First();

            book.Should().NotBeNull();

            var payload = new
            {
                Title = book.Title + " | Patch Test",
            };

            var bookEditRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(new HttpMethod("PATCH"), resource: $"/Books({book.Id})", payload: payload, acceptHeader: WebApiConstants.DefaultAcceptHeader);
            bookEditRequest.IsSuccessStatusCode.Should().BeTrue();

            var bookCheckRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: $"/Books({book.Id})", acceptHeader: ODataConstants.DefaultAcceptHeader);
            bookEditRequest.IsSuccessStatusCode.Should().BeTrue();
            var (book2, errorContent2) = await bookCheckRequest.DeserializeResponseAsync<Book>();
            book2.Should().NotBeNull();
            book2.Title.Should().EndWith(" | Patch Test");
        }
    }
}