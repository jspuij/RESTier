// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
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
    {
        var id = $"{name}_{Guid.NewGuid():N}";
        return id.Length > 64 ? id[..64] : id;
    }

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

    [Fact]
    public async Task DeepUpdate_SingleNavProperty_ReplaceWithExisting()
    {
        // Create a Book linked to Publisher1
        var bookPayload = new { Isbn = "3030303030303", Title = "NavProp Replace Test", IsActive = true };
        var createResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers('Publisher1')/Books",
            payload: bookPayload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        createResponse.IsSuccessStatusCode.Should().BeTrue();
        var (createdBook, _) = await createResponse.DeserializeResponseAsync<Book>();

        // PATCH with Publisher2 inline (has key + non-key props → classified as Update+link)
        // NOTE: Must include at least one non-key property; key-only payloads are treated
        // as entity references (@odata.bind) by IsEntityReference and never reach the classifier.
        var patchPayload = new
        {
            Publisher = new { Id = "Publisher2", Addr = new { Street = "456 Oak Ave", Zip = "54321" } },
        };
        var patchResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            new HttpMethod("PATCH"),
            resource: $"/Books({createdBook.Id})",
            payload: patchPayload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        var content = await patchResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        patchResponse.IsSuccessStatusCode.Should().BeTrue(
            because: $"replacing Publisher via inline nested entity should succeed. Response: {content}");

        // Verify book is now linked to Publisher2
        var verifyResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Books({createdBook.Id})?$expand=Publisher",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        var (updatedBook, _) = await verifyResponse.DeserializeResponseAsync<Book>();
        updatedBook.PublisherId.Should().Be("Publisher2");
    }

    [Fact]
    public async Task DeepUpdate_MoveExistingChildToNewParent()
    {
        // Create two publishers, each with one book
        var pubA = UniqueId();
        var pubB = UniqueId();

        // Create publisher A with a book
        var createA = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post, resource: "/Publishers",
            payload: new { Id = pubA, Addr = new { Zip = "00000" },
                Books = new[] { new { Isbn = "1111100000111", Title = "Book A", IsActive = true } } },
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        createA.IsSuccessStatusCode.Should().BeTrue();

        // Create publisher B with a book
        var createB = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post, resource: "/Publishers",
            payload: new { Id = pubB, Addr = new { Zip = "00000" },
                Books = new[] { new { Isbn = "2222200000222", Title = "Book B", IsActive = true } } },
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        createB.IsSuccessStatusCode.Should().BeTrue();

        // Get Book B's ID
        var getBResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get, resource: $"/Publishers('{pubB}')?$expand=Books",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        var (publisherB, _) = await getBResponse.DeserializeResponseAsync<Publisher>();
        var bookBId = publisherB.Books[0].Id;

        // PATCH Publisher A with Book B (by key) — should move it
        var patchPayload = new
        {
            Books = new[]
            {
                new { Id = bookBId, Isbn = "2222200000222", Title = "Book B Moved", IsActive = true },
            },
        };

        var patchResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            new HttpMethod("PATCH"), resource: $"/Publishers('{pubA}')",
            payload: patchPayload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        var patchContent = await patchResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        patchResponse.IsSuccessStatusCode.Should().BeTrue(
            because: $"moving book to new publisher should succeed. Response: {patchContent}");

        // Verify: book is now linked to Publisher A
        var verifyResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get, resource: $"/Books({bookBId})?$expand=Publisher",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        var (movedBook, _) = await verifyResponse.DeserializeResponseAsync<Book>();
        movedBook.PublisherId.Should().Be(pubA, because: "book should now be linked to Publisher A");
    }

    [Fact]
    public async Task DeepUpdate_FiresConventionMethods()
    {
        // Create a Book linked to Publisher1
        var bookPayload = new { Isbn = "5050505050505", Title = "Convention Fire Test", IsActive = true };
        var createResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers('Publisher1')/Books",
            payload: bookPayload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        var createContent = await createResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        createResponse.IsSuccessStatusCode.Should().BeTrue(
            because: $"creating the book should succeed. Response: {createContent}");
        var (createdBook, _) = await createResponse.DeserializeResponseAsync<Book>();

        // Get Publisher1's current LastUpdated timestamp
        var pubResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Publishers('Publisher1')",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        pubResponse.IsSuccessStatusCode.Should().BeTrue();
        var (publisher, _) = await pubResponse.DeserializeResponseAsync<Publisher>();
        var lastUpdatedBefore = publisher.LastUpdated;

        // PATCH the Book with Publisher1 inline (key + non-key props → reclassified as Update).
        // OnUpdatingPublisher should fire and set LastUpdated to DateTimeOffset.Now.
        var patchPayload = new
        {
            Publisher = new { Id = "Publisher1", Addr = new { Street = "Updated St", Zip = "11111" } },
        };
        var patchResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            new HttpMethod("PATCH"),
            resource: $"/Books({createdBook.Id})",
            payload: patchPayload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        var patchContent = await patchResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        patchResponse.IsSuccessStatusCode.Should().BeTrue(
            because: $"PATCH with inline publisher update should succeed. Response: {patchContent}");

        // Verify: Publisher1.LastUpdated has changed (OnUpdatingPublisher fired)
        var verifyResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Publishers('Publisher1')",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        verifyResponse.IsSuccessStatusCode.Should().BeTrue();

        var (updatedPublisher, _) = await verifyResponse.DeserializeResponseAsync<Publisher>();
        updatedPublisher.LastUpdated.Should().BeAfter(lastUpdatedBefore,
            because: "OnUpdatingPublisher should have set LastUpdated to DateTimeOffset.Now during the deep update");
    }

    // Case A: Single-nav insert with server-generated key (no key in payload).
    // Testable via Review.Book (Book.Id is server-generated via OnInsertingBook).
    // However, this currently fails because FindTargetEntitySet falls back to the type name
    // "Book" instead of the entity set name "Books" when the Reviews entity set doesn't have
    // an explicit navigation binding for Review.Book in the OData model. The EF initializer
    // then NREs on dbContext.GetType().GetProperty("Book") since the property is "Books".
    // The ExtractKeyValues fix (filtering by GetChangedPropertyNames) is in place, so the
    // classifier correctly treats keyless payloads as no-key Inserts. The remaining gap is
    // entity set name resolution in FindTargetEntitySet, which is a pre-existing infrastructure
    // issue beyond Phase 3 scope.

    [Fact]
    public async Task DeepUpdate_SingleNavProperty_InsertNewRelated_ClientSuppliedKey()
    {
        // Create a Book linked to Publisher1
        var bookPayload = new { Isbn = "4040404040404", Title = "NavProp Insert Test", IsActive = true };
        var createResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers('Publisher1')/Books",
            payload: bookPayload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        createResponse.IsSuccessStatusCode.Should().BeTrue();
        var (createdBook, _) = await createResponse.DeserializeResponseAsync<Book>();

        // PATCH with a NEW Publisher (client-supplied key, doesn't exist in DB).
        // Must include non-key properties to avoid IsEntityReference heuristic.
        var newPubId = UniqueId();
        var patchPayload = new
        {
            Publisher = new { Id = newPubId, Addr = new { Street = "789 New St", Zip = "99999" } },
        };
        var patchResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            new HttpMethod("PATCH"),
            resource: $"/Books({createdBook.Id})",
            payload: patchPayload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        var content = await patchResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        patchResponse.IsSuccessStatusCode.Should().BeTrue(
            because: $"inserting new Publisher via inline nested entity should succeed. Response: {content}");

        // Verify: new publisher exists and book is linked to it
        var verifyResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Books({createdBook.Id})?$expand=Publisher",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        var (updatedBook, _) = await verifyResponse.DeserializeResponseAsync<Book>();
        updatedBook.PublisherId.Should().Be(newPubId);
        updatedBook.Publisher.Should().NotBeNull();
        updatedBook.Publisher.Addr.Should().NotBeNull();
        updatedBook.Publisher.Addr.Street.Should().Be("789 New St");
    }

    [Fact]
    public async Task Post_ODataVersion401_ReturnsClearErrorMessage()
    {
        var server = RestierTestHelpers.GetTestableRestierServer<TApi>(
            apiServiceCollection: ConfigureServices);
        var client = server.CreateClient();

        var payload = new { Id = "test", Addr = new { Zip = "00000" } };
        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/tests/Publishers")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("OData-Version", "4.01");
        request.Headers.Add("Accept", "application/json");

        var response = await client.SendAsync(request, TestContext.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        content.Should().Contain("4.01 is not supported");
    }

    [Fact]
    public async Task Patch_ODataVersion401_ReturnsClearErrorMessage()
    {
        // PATCH with OData-Version: 4.01 triggers deserialization failure (edmEntityObject = null).
        // If-Match: * satisfies GetOriginalValues (returns empty dict at line 826-828),
        // so the request reaches the edmEntityObject null guard in Update().
        // Use a seeded book so the OData routing resolves the entity set correctly.
        var existingBookId = new Guid("19d68c75-1313-4369-b2bf-521f2b260a59");

        var server = RestierTestHelpers.GetTestableRestierServer<TApi>(
            apiServiceCollection: ConfigureServices);
        var client = server.CreateClient();

        var payload = new { Title = "Test" };
        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"http://localhost/api/tests/Books({existingBookId})")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("OData-Version", "4.01");
        request.Headers.Add("If-Match", "*");
        request.Headers.Add("Accept", "application/json");

        var response = await client.SendAsync(request, TestContext.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        content.Should().Contain("4.01 is not supported");
    }
}
