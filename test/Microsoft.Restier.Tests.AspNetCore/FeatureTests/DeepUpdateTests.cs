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
}
