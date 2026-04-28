// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.EasyAF.Http.OData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

public abstract class DeepUpdateTests<TApi, TContext> : RestierTestBase<TApi>
    where TApi : ApiBase
    where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    private static string UniqueId([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        => $"{name}_{Guid.NewGuid():N}"[..50];

    /// <summary>
    /// JsonSerializerOptions that include null values in the output,
    /// overriding Breakdance's default of <see cref="JsonIgnoreCondition.WhenWritingNull"/>.
    /// </summary>
    private static readonly JsonSerializerOptions IncludeNulls = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    [Fact]
    public async Task DeepUpdate_PatchBookTitle()
    {
        // GET a book to find its id and original title
        var bookRequest = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Books?$top=1",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        bookRequest.IsSuccessStatusCode.Should().BeTrue();

        var (bookList, _) = await bookRequest.DeserializeResponseAsync<ODataV4List<Book>>();
        bookList.Should().NotBeNull();
        bookList.Items.Should().NotBeNullOrEmpty();

        var book = bookList.Items.First();
        var originalTitle = book.Title;

        // PATCH with a new title
        var payload = new
        {
            Title = $"{originalTitle} | DeepUpdate Test",
        };

        var patchResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            new HttpMethod("PATCH"),
            resource: $"/Books({book.Id})",
            payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        var patchContent = await patchResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        patchResponse.IsSuccessStatusCode.Should().BeTrue(
            because: $"PATCH should succeed. Response body: {patchContent}");

        // GET again and verify the title changed
        var checkResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Books({book.Id})",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        checkResponse.IsSuccessStatusCode.Should().BeTrue();

        var (updatedBook, _) = await checkResponse.DeserializeResponseAsync<Book>();
        updatedBook.Should().NotBeNull();
        updatedBook.Title.Should().Be($"{originalTitle} | DeepUpdate Test");

        // Cleanup: restore original title
        var cleanupPayload = new
        {
            Title = originalTitle,
        };
        var cleanupResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            new HttpMethod("PATCH"),
            resource: $"/Books({book.Id})",
            payload: cleanupPayload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        cleanupResponse.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task DeepUpdate_NullUnlinks_V40()
    {
        // GET a book that has a publisher
        var bookRequest = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Books?$expand=Publisher&$top=1",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        bookRequest.IsSuccessStatusCode.Should().BeTrue();

        var (bookList, _) = await bookRequest.DeserializeResponseAsync<ODataV4List<Book>>();
        bookList.Should().NotBeNull();
        bookList.Items.Should().NotBeNullOrEmpty();

        var book = bookList.Items.First();
        book.Publisher.Should().NotBeNull(because: "the seeded book should have a publisher");
        var originalPublisherId = book.PublisherId;

        // PATCH with PublisherId = null to unlink the publisher.
        // Must use IncludeNulls so that the null value is actually serialized into the JSON body;
        // Breakdance's default JsonSerializerOptions use WhenWritingNull which would omit it.
        var payload = new
        {
            PublisherId = (string)null,
        };

        var patchResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            new HttpMethod("PATCH"),
            resource: $"/Books({book.Id})",
            payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices,
            jsonSerializerSettings: IncludeNulls);

        var patchContent = await patchResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        patchResponse.IsSuccessStatusCode.Should().BeTrue(
            because: $"PATCH with null FK should succeed. Response body: {patchContent}");

        // GET again with $expand=Publisher and verify Publisher is null
        var checkResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Books({book.Id})?$expand=Publisher",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        checkResponse.IsSuccessStatusCode.Should().BeTrue();

        var (updatedBook, _) = await checkResponse.DeserializeResponseAsync<Book>();
        updatedBook.Should().NotBeNull();
        updatedBook.PublisherId.Should().BeNull(because: "the publisher FK was set to null");
        updatedBook.Publisher.Should().BeNull(because: "the publisher was unlinked");

        // Cleanup: restore the original publisher link
        var cleanupPayload = new
        {
            PublisherId = originalPublisherId,
        };
        var cleanupResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            new HttpMethod("PATCH"),
            resource: $"/Books({book.Id})",
            payload: cleanupPayload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        cleanupResponse.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task DeepUpdate_InlineNewChildWithoutKey_Inserts()
    {
        // Create a publisher, then PATCH it with an inline new Book (no Id)
        var pubId = UniqueId();
        var createPayload = new
        {
            Id = pubId,
            Addr = new { Zip = "00000" },
        };

        var createResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers",
            payload: createPayload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        var createContent = await createResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            because: $"creating the publisher should succeed. Response: {createContent}");

        // PATCH with inline new Book (no Id means server-generated key -> Insert)
        var patchPayload = new
        {
            Books = new[]
            {
                new { Isbn = "5551234567890", Title = "Deep Update Insert Book", IsActive = true },
            },
        };

        var patchResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            new HttpMethod("PATCH"),
            resource: $"/Publishers('{pubId}')",
            payload: patchPayload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        var patchContent = await patchResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        patchResponse.IsSuccessStatusCode.Should().BeTrue(
            because: $"PATCH with inline new book should succeed. Response: {patchContent}");

        // Verify the book was inserted
        var getResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Publishers('{pubId}')?$expand=Books",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        getResponse.IsSuccessStatusCode.Should().BeTrue();

        var (publisher, _) = await getResponse.DeserializeResponseAsync<Publisher>();
        publisher.Should().NotBeNull();
        publisher.Books.Should().HaveCount(1);
        publisher.Books[0].Title.Should().Be("Deep Update Insert Book");
        publisher.Books[0].Id.Should().NotBe(Guid.Empty,
            because: "OnInsertingBook should have assigned a server-generated Guid");
    }

    [Fact]
    public async Task DeepUpdate_Put_OmittedChildrenUnlinked()
    {
        // Create a publisher with 2 books via deep insert
        var pubId = UniqueId();
        var createPayload = new
        {
            Id = pubId,
            Addr = new { Zip = "00000" },
            Books = new[]
            {
                new { Isbn = "6661234567890", Title = "Keep This Book", IsActive = true },
                new { Isbn = "6669876543210", Title = "Omit This Book", IsActive = true },
            },
        };

        var createResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers",
            payload: createPayload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        var createContent = await createResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            because: $"deep insert should succeed. Response: {createContent}");

        // GET to retrieve both book IDs
        var getResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Publishers('{pubId}')?$expand=Books",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        getResponse.IsSuccessStatusCode.Should().BeTrue();

        var (publisher, _) = await getResponse.DeserializeResponseAsync<Publisher>();
        publisher.Should().NotBeNull();
        publisher.Books.Should().HaveCount(2);

        var keepBook = publisher.Books.First(b => b.Title == "Keep This Book");
        var omitBook = publisher.Books.First(b => b.Title == "Omit This Book");

        // PUT with only 1 book — the other should be unlinked
        var putPayload = new
        {
            Id = pubId,
            Addr = new { Zip = "00000" },
            Books = new[]
            {
                new { Id = keepBook.Id, Isbn = keepBook.Isbn, Title = keepBook.Title, IsActive = keepBook.IsActive },
            },
        };

        var putResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Put,
            resource: $"/Publishers('{pubId}')",
            payload: putPayload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        var putContent = await putResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        putResponse.IsSuccessStatusCode.Should().BeTrue(
            because: $"PUT with only 1 book should succeed. Response: {putContent}");

        // Verify the publisher now has only 1 book
        var verifyResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Publishers('{pubId}')?$expand=Books",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        verifyResponse.IsSuccessStatusCode.Should().BeTrue();

        var (updatedPublisher, _) = await verifyResponse.DeserializeResponseAsync<Publisher>();
        updatedPublisher.Should().NotBeNull();
        updatedPublisher.Books.Should().HaveCount(1);
        updatedPublisher.Books[0].Id.Should().Be(keepBook.Id);

        // Verify the omitted book still exists (not deleted) but has no publisher
        var omitBookResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Books({omitBook.Id})",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        omitBookResponse.IsSuccessStatusCode.Should().BeTrue(
            because: "the omitted book should still exist in the database");

        var (omittedBook, _) = await omitBookResponse.DeserializeResponseAsync<Book>();
        omittedBook.Should().NotBeNull();
        omittedBook.PublisherId.Should().BeNull(
            because: "the non-contained omitted book should have its FK set to null (unlinked, not deleted)");
    }
}
