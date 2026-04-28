// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.EasyAF.Http.OData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

public abstract class DeepInsertTests<TApi, TContext> : RestierTestBase<TApi>
    where TApi : ApiBase
    where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    private static string UniqueId([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        => $"{name}_{Guid.NewGuid():N}"[..50];

    [Fact]
    public async Task DeepInsert_CollectionNavProperty()
    {
        var pubId = UniqueId();
        var payload = new
        {
            Id = pubId,
            Addr = new { Zip = "00000" },
            Books = new[]
            {
                new { Isbn = "1234567890123", Title = "Deep Book 1", IsActive = true },
                new { Isbn = "9876543210123", Title = "Deep Book 2", IsActive = true },
            },
        };

        var postResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers",
            payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        var postContent = await postResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            because: $"deep insert POST should succeed. Response body: {postContent}");

        // Verify the publisher was created with its books
        var getResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Publishers('{pubId}')?$expand=Books",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        getResponse.IsSuccessStatusCode.Should().BeTrue();

        var (publisher, _) = await getResponse.DeserializeResponseAsync<Publisher>();
        publisher.Should().NotBeNull();
        publisher.Id.Should().Be(pubId);
        publisher.Books.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeepInsert_ServerGeneratedKeys()
    {
        var pubId = UniqueId();
        var payload = new
        {
            Id = pubId,
            Addr = new { Zip = "00000" },
            Books = new[]
            {
                new { Isbn = "1111111111111", Title = "Server Key Book", IsActive = true },
            },
        };

        var postResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers",
            payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        var postContent = await postResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            because: $"deep insert POST should succeed. Response body: {postContent}");

        // Verify the book got a server-generated Guid from OnInsertingBook
        var getResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Publishers('{pubId}')?$expand=Books",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        getResponse.IsSuccessStatusCode.Should().BeTrue();

        var (publisher, _) = await getResponse.DeserializeResponseAsync<Publisher>();
        publisher.Should().NotBeNull();
        publisher.Books.Should().HaveCount(1);
        publisher.Books[0].Id.Should().NotBe(Guid.Empty,
            because: "OnInsertingBook should have assigned a server-generated Guid");
    }

    [Fact]
    public async Task DeepInsert_FiresConventionMethods()
    {
        // Post with a Book that has Id = Guid.Empty, which OnInsertingBook should replace with a real Guid
        var pubId = UniqueId();
        var payload = new
        {
            Id = pubId,
            Addr = new { Zip = "00000" },
            Books = new[]
            {
                new { Id = Guid.Empty, Isbn = "2222222222222", Title = "Convention Book", IsActive = true },
            },
        };

        var postResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers",
            payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        var postContent = await postResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            because: $"deep insert POST should succeed. Response body: {postContent}");

        // Verify the convention method fired and assigned a non-empty Guid
        var getResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Publishers('{pubId}')?$expand=Books",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        getResponse.IsSuccessStatusCode.Should().BeTrue();

        var (publisher, _) = await getResponse.DeserializeResponseAsync<Publisher>();
        publisher.Should().NotBeNull();
        publisher.Books.Should().HaveCount(1);
        publisher.Books[0].Id.Should().NotBe(Guid.Empty,
            because: "OnInsertingBook convention should have assigned a non-empty Guid");
    }

    [Fact]
    public async Task DeepInsert_MaxDepth1_AllowsOneLevel()
    {
        var pubId = UniqueId();
        var payload = new
        {
            Id = pubId,
            Addr = new { Zip = "00000" },
            Books = new[]
            {
                new { Isbn = "6666666666666", Title = "Depth 1 OK Book", IsActive = true },
            },
        };

        var postResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers",
            payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: services =>
            {
                ConfigureServices(services);
                services.AddSingleton(new DeepOperationSettings { MaxDepth = 1 });
            });

        var postContent = await postResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            because: $"MaxDepth=1 should allow one level of nesting. Response: {postContent}");
    }

    [Fact]
    public async Task DeepInsert_ExceedsMaxDepth_Returns400()
    {
        // A payload with 2 levels of nesting: Publisher -> Books -> Reviews
        var pubId = UniqueId();
        var payload = new
        {
            Id = pubId,
            Addr = new { Zip = "00000" },
            Books = new[]
            {
                new
                {
                    Isbn = "3333333333333",
                    Title = "Too Deep Book",
                    IsActive = true,
                    Reviews = new[]
                    {
                        new { Content = "Great book!", Rating = 5 },
                    },
                },
            },
        };

        // Override DeepOperationSettings to set MaxDepth = 1, allowing only 1 level of nesting
        Action<IServiceCollection> configureWithMaxDepth = services =>
        {
            ConfigureServices(services);
            services.AddSingleton(new DeepOperationSettings { MaxDepth = 1 });
        };

        var postResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers",
            payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: configureWithMaxDepth);

        postResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            because: "nesting depth exceeds MaxDepth=1 (Publisher->Books is OK at depth 0, but Books->Reviews at depth 1 should be rejected)");
    }

    [Fact]
    public async Task DeepInsert_WithBindReference()
    {
        // When a nested entity contains only key properties, the key-subset heuristic
        // detects it as a bind reference (@odata.bind / entity reference) rather than a
        // new entity to insert. This test verifies that a Publisher can be created with
        // an existing Book wired via bind reference (only the Book's key is supplied).
        var pubId = UniqueId();

        // "A Clockwork Orange" — seeded, active, belongs to Publisher1.
        // We re-bind it to a new publisher to exercise the bind reference resolution path.
        var existingBookId = new Guid("19d68c75-1313-4369-b2bf-521f2b260a59");

        var payload = new
        {
            Id = pubId,
            Addr = new { Zip = "11111" },
            Books = new object[]
            {
                new { Id = existingBookId }, // Only key property — detected as bind reference
            },
        };

        var postResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers",
            payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        var postContent = await postResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            because: $"POST with a bind reference to an existing Book should succeed. Response: {postContent}");

        // Verify the new publisher was created
        var getResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Publishers('{pubId}')",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        getResponse.IsSuccessStatusCode.Should().BeTrue(
            because: "the newly created Publisher should be retrievable");

        var (publisher, _) = await getResponse.DeserializeResponseAsync<Publisher>();
        publisher.Should().NotBeNull();
        publisher.Id.Should().Be(pubId);
    }

    [Fact]
    public async Task DeepInsert_BindReferenceNotFound_Returns400()
    {
        // When a nested entity is detected as a bind reference (only key properties)
        // but the referenced entity does not exist, Phase 1 bind validation must return 400.
        var pubId = UniqueId();
        var nonExistentBookId = Guid.NewGuid();

        var payload = new
        {
            Id = pubId,
            Addr = new { Zip = "00000" },
            Books = new object[]
            {
                new { Id = nonExistentBookId }, // Only key property — detected as bind reference
            },
        };

        var postResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers",
            payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        var postContent = await postResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        postResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            because: $"referencing a non-existent Book as a bind reference should return 400. Response: {postContent}");
    }
}
