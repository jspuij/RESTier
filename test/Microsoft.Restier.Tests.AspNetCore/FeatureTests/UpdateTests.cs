// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using CloudNimble.EasyAF.Http.OData;
using FluentAssertions;
using CloudNimble.Breakdance.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

public class UpdateTests : RestierTestBase<LibraryApi>
{
    [Fact]
    public async Task UpdateBookWithPublisher_ShouldReturn400()
    {
        var bookRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/Books?$expand=Publisher&$top=1",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
        bookRequest.IsSuccessStatusCode.Should().BeTrue();

        var (bookList, _) = await bookRequest.DeserializeResponseAsync<ODataV4List<Book>>();
        bookList.Should().NotBeNull();
        bookList.Items.Should().NotBeNullOrEmpty();

        var book = bookList.Items.First();
        book.Should().NotBeNull();
        book.Publisher.Should().NotBeNull();
        book.Title += " Test";

        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Put,
            resource: $"/Books({book.Id})",
            payload: book,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateBook()
    {
        var bookRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/Books?$top=1",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
        bookRequest.IsSuccessStatusCode.Should().BeTrue();

        var (bookList, _) = await bookRequest.DeserializeResponseAsync<ODataV4List<Book>>();
        var book = bookList.Items.First();
        var originalTitle = book.Title;
        book.Title += " Test";

        var updateResponse = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Put,
            resource: $"/Books({book.Id})",
            payload: book,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
        updateResponse.IsSuccessStatusCode.Should().BeTrue();

        var checkResponse = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: $"/Books({book.Id})",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
        checkResponse.IsSuccessStatusCode.Should().BeTrue();

        var (updatedBook, _) = await checkResponse.DeserializeResponseAsync<Book>();
        updatedBook.Should().NotBeNull();
        updatedBook.Title.Should().Be($"{originalTitle} Test");

        await Cleanup(book.Id, originalTitle);
    }

    [Fact]
    public async Task PatchBook()
    {
        var bookRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/Books?$top=1",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
        bookRequest.IsSuccessStatusCode.Should().BeTrue();

        var (bookList, _) = await bookRequest.DeserializeResponseAsync<ODataV4List<Book>>();
        var book = bookList.Items.First();
        var originalTitle = book.Title;

        var payload = new
        {
            Title = $"{book.Title} | Patch Test",
        };

        var patchResponse = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            new HttpMethod("PATCH"),
            resource: $"/Books({book.Id})",
            payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
        patchResponse.IsSuccessStatusCode.Should().BeTrue();

        var checkResponse = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: $"/Books({book.Id})",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
        checkResponse.IsSuccessStatusCode.Should().BeTrue();

        var (updatedBook, _) = await checkResponse.DeserializeResponseAsync<Book>();
        updatedBook.Should().NotBeNull();
        updatedBook.Title.Should().Be($"{originalTitle} | Patch Test");

        await Cleanup(book.Id, originalTitle);
    }

    [Fact]
    public async Task UpdatePublisher_ShouldCallInterceptor()
    {
        var publisherRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/Publishers('Publisher1')",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
        publisherRequest.IsSuccessStatusCode.Should().BeTrue();

        var (publisher, _) = await publisherRequest.DeserializeResponseAsync<Publisher>();
        publisher.Should().NotBeNull();
        publisher.LastUpdated.Should().NotBeCloseTo(DateTimeOffset.Now, new TimeSpan(0, 0, 0, 5));

        publisher.Books = null;

        var updateResponse = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Put,
            resource: $"/Publishers('{publisher.Id}')",
            payload: publisher,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
        _ = await TraceListener.LogAndReturnMessageContentAsync(updateResponse);

        updateResponse.IsSuccessStatusCode.Should().BeTrue();

        var checkResponse = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/Publishers('Publisher1')",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
        checkResponse.IsSuccessStatusCode.Should().BeTrue();

        var (updatedPublisher, _) = await checkResponse.DeserializeResponseAsync<Publisher>();
        updatedPublisher.Should().NotBeNull();
        updatedPublisher.LastUpdated.Should().BeCloseTo(DateTimeOffset.Now, new TimeSpan(0, 0, 0, 6));
    }

    private static async Task Cleanup(Guid bookId, string title)
    {
        var api = await RestierTestHelpers.GetTestableApiInstance<LibraryApi>(
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
        var book = api.DbContext.Books.First(candidate => candidate.Id == bookId);
        book.Title = title;
        await api.DbContext.SaveChangesAsync();
    }
}
