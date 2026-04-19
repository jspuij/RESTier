// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.EasyAF.Http.OData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

/// <summary>
/// Integration tests verifying that <see cref="RestierNamingConvention.LowerCamelCase"/> and
/// <see cref="RestierNamingConvention.LowerCamelCaseWithEnumMembers"/> work end-to-end.
/// Tests use /Readers (no OnFilter convention) for GET assertions to avoid dependence on
/// shared mutable in-memory DB state for the Books entity set.
/// </summary>
public abstract class NamingConventionTests<TApi, TContext> : RestierTestBase<TApi> where TApi : ApiBase where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    private static readonly JsonSerializerOptions CamelCaseSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly JsonSerializerOptions CamelCaseDeserializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    #region GET / Query

    [Fact]
    public async Task GetEntitySet_ReturnsCamelCasePropertyNames()
    {
        // Use /Readers which has seeded data and no OnFilter convention
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get, resource: "/Readers", serviceCollection: ConfigureServices,
            namingConvention: RestierNamingConvention.LowerCamelCase);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("\"fullName\"");
        content.Should().Contain("\"id\"");
        content.Should().NotContain("\"FullName\"");
        content.Should().NotContain("\"Id\":");
    }

    [Fact]
    public async Task GetMetadata_ShowsCamelCasePropertyNames()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get, resource: "/$metadata", acceptHeader: "application/xml",
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("Name=\"title\"");
        content.Should().Contain("Name=\"isbn\"");
        content.Should().Contain("Name=\"isActive\"");
        content.Should().Contain("Name=\"fullName\"");
    }

    [Fact]
    public async Task GetWithSelect_WorksWithCamelCase()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get, resource: "/Readers?$select=fullName",
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("\"fullName\"");
    }

    [Fact]
    public async Task GetWithFilter_WorksWithCamelCase()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get, resource: "/Readers?$filter=fullName eq 'p1'",
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("\"p1\"");
    }

    [Fact]
    public async Task GetWithExpand_WorksWithCamelCase()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get, resource: "/Publishers?$expand=books",
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("\"books\"");
    }

    [Fact]
    public async Task GetWithOrderBy_WorksWithCamelCase()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get, resource: "/Readers?$orderby=fullName",
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    #endregion

    #region Key Handling

    [Fact]
    public async Task GetByKey_WorksWithCamelCase()
    {
        // POST a book first so we have a known entity
        var book = new Book { Title = "Key Test Book", Isbn = "1111111111111" };
        var insertResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post, resource: "/Publishers('Publisher1')/Books", payload: book,
            acceptHeader: WebApiConstants.DefaultAcceptHeader, jsonSerializerSettings: CamelCaseSerializerOptions,
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        insertResponse.IsSuccessStatusCode.Should().BeTrue();
        var (createdBook, _) = await insertResponse.DeserializeResponseAsync<Book>(CamelCaseDeserializerOptions);

        // GET by key
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get, resource: $"/Books({createdBook.Id})", acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("\"title\"");
        content.Should().Contain("Key Test Book");
    }

    [Fact]
    public async Task DeleteByKey_WorksWithCamelCase()
    {
        // POST a book first
        var book = new Book { Title = "Book To Delete", Isbn = "9999999999999" };
        var insertResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post, resource: "/Publishers('Publisher1')/Books", payload: book,
            acceptHeader: WebApiConstants.DefaultAcceptHeader, jsonSerializerSettings: CamelCaseSerializerOptions,
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        insertResponse.IsSuccessStatusCode.Should().BeTrue();
        var (createdBook, _) = await insertResponse.DeserializeResponseAsync<Book>(CamelCaseDeserializerOptions);

        // DELETE by key
        var deleteResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Delete, resource: $"/Books({createdBook.Id})",
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    #endregion

    #region POST / PATCH / PUT

    [Fact]
    public async Task PostBook_WithDefaultSerialization_CreatesEntity()
    {
        // OData deserializer matches properties by EDM name. With camelCase EDM,
        // the default PascalCase serialization still works because OData's model
        // binder is case-insensitive for property matching.
        var book = new Book { Title = "CamelCase Insert Test", Isbn = "0118006345789" };
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post, resource: "/Publishers('Publisher1')/Books", payload: book,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);
        content.Should().Contain("\"title\"");
        content.Should().Contain("CamelCase Insert Test");
    }

    [Fact]
    public async Task PostBook_WithCamelCasePayload_CreatesEntity()
    {
        var book = new Book { Title = "CamelCase Explicit Test", Isbn = "0118006345790" };
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post, resource: "/Publishers('Publisher1')/Books", payload: book,
            acceptHeader: WebApiConstants.DefaultAcceptHeader, jsonSerializerSettings: CamelCaseSerializerOptions,
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);
        content.Should().Contain("\"title\"");
        content.Should().Contain("CamelCase Explicit Test");
    }

    [Fact]
    public async Task PatchBook_WithCamelCasePayload_UpdatesEntity()
    {
        // POST a book first
        var book = new Book { Title = "Original Patch Title", Isbn = "2222222222222" };
        var insertResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post, resource: "/Publishers('Publisher1')/Books", payload: book,
            acceptHeader: WebApiConstants.DefaultAcceptHeader, jsonSerializerSettings: CamelCaseSerializerOptions,
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        insertResponse.IsSuccessStatusCode.Should().BeTrue();
        var (createdBook, _) = await insertResponse.DeserializeResponseAsync<Book>(CamelCaseDeserializerOptions);

        // PATCH with camelCase anonymous payload
        var payload = new { title = "Patched CamelCase Title" };
        var patchResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            new HttpMethod("PATCH"), resource: $"/Books({createdBook.Id})", payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        patchResponse.IsSuccessStatusCode.Should().BeTrue();

        // Verify the change persisted
        var checkResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get, resource: $"/Books({createdBook.Id})", acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        checkResponse.IsSuccessStatusCode.Should().BeTrue();
        var (updatedBook, _) = await checkResponse.DeserializeResponseAsync<Book>(CamelCaseDeserializerOptions);
        updatedBook.Title.Should().Be("Patched CamelCase Title");
    }

    [Fact]
    public async Task PutBook_WithCamelCasePayload_ReplacesEntity()
    {
        // POST a book first
        var book = new Book { Title = "Original Put Title", Isbn = "3333333333333" };
        var insertResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post, resource: "/Publishers('Publisher1')/Books", payload: book,
            acceptHeader: WebApiConstants.DefaultAcceptHeader, jsonSerializerSettings: CamelCaseSerializerOptions,
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        insertResponse.IsSuccessStatusCode.Should().BeTrue();
        var (createdBook, _) = await insertResponse.DeserializeResponseAsync<Book>(CamelCaseDeserializerOptions);
        createdBook.Title = "Replaced CamelCase Title";

        // PUT with camelCase payload
        var putResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Put, resource: $"/Books({createdBook.Id})", payload: createdBook,
            acceptHeader: WebApiConstants.DefaultAcceptHeader, jsonSerializerSettings: CamelCaseSerializerOptions,
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        putResponse.IsSuccessStatusCode.Should().BeTrue();

        // Verify the change persisted
        var checkResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get, resource: $"/Books({createdBook.Id})", acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        checkResponse.IsSuccessStatusCode.Should().BeTrue();
        var (updatedBook, _) = await checkResponse.DeserializeResponseAsync<Book>(CamelCaseDeserializerOptions);
        updatedBook.Title.Should().Be("Replaced CamelCase Title");
    }

    #endregion

    #region Concurrency (ETag)

    [Fact]
    public async Task GetLibraryCard_WithCamelCase_ReturnsCamelCaseAndETag()
    {
        var getResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get, resource: "/LibraryCards(a1111111-1111-1111-1111-111111111111)",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        getResponse.IsSuccessStatusCode.Should().BeTrue();
        var content = await TraceListener.LogAndReturnMessageContentAsync(getResponse);
        content.Should().Contain("\"dateRegistered\"");
        var etag = getResponse.Headers.ETag;
        etag.Should().NotBeNull("LibraryCard has [ConcurrencyCheck] so responses should include ETag");
    }

    #endregion

    #region Enum Members

    [Fact]
    public async Task PostBook_WithCamelCaseEnumValue_CreatesEntity()
    {
        var payload = new { title = "Enum Test Book", isbn = "5555555555555", category = "fiction" };
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post, resource: "/Publishers('Publisher1')/Books", payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCaseWithEnumMembers);
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);
        content.Should().Contain("fiction");
    }

    [Fact]
    public async Task GetMetadata_WithEnumMembers_ShowsCamelCaseEnumValues()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get, resource: "/$metadata", acceptHeader: "application/xml",
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCaseWithEnumMembers);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.IsSuccessStatusCode.Should().BeTrue();
        // With LowerCamelCaseWithEnumMembers, enum member names should be camelCase in metadata
        content.Should().Contain("Name=\"fiction\"");
    }

    #endregion
}
