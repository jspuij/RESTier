// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using CloudNimble.EasyAF.Http.OData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

[Collection("LibraryApi")]
public class NavigationPropertyTests : RestierTestBase<LibraryApi>
{
    [Fact]
    public async Task NavigationProperties_ChildrenShouldFilter_IsActive()
    {
        var context = await RestierTestHelpers.GetTestableInjectedService<LibraryApi, LibraryContext>(
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());

        var publisher = new Publisher
        {
            Id = "navtest-publisher-1",
            Books =
            [
                new Book { Id = Guid.NewGuid(), Title = "navtest-pub1-book-1", IsActive = true },
                new Book { Id = Guid.NewGuid(), Title = "navtest-pub1-book-2", IsActive = false },
            ],
            Addr = new Shared.Scenarios.Library.Address { Zip = "12345" },
        };
        context.Publishers.Add(publisher);
        context.SaveChanges();

        try
        {
            var request = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
                HttpMethod.Get,
                resource: $"/Publishers('{publisher.Id}')?$expand=Books",
                acceptHeader: ODataConstants.DefaultAcceptHeader,
                serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
            request.IsSuccessStatusCode.Should().BeTrue();

            var (expandedPublisher, _) = await request.DeserializeResponseAsync<Publisher>();
            expandedPublisher.Should().NotBeNull();
            expandedPublisher.Books.Should().HaveCount(1);

            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
                HttpMethod.Get,
                resource: $"/Publishers('{publisher.Id}')/Books",
                serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
            response.IsSuccessStatusCode.Should().BeTrue();

            var (books, _) = await response.DeserializeResponseAsync<ODataV4List<Book>>();
            books.Items.Should().HaveCount(1);
        }
        finally
        {
            CleanupPublisher(context, publisher);
        }
    }

    [Fact]
    public async Task NavigationProperties_ChildrenShouldFilter_Explicit()
    {
        var context = await RestierTestHelpers.GetTestableInjectedService<LibraryApi, LibraryContext>(
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());

        var publisher = new Publisher
        {
            Id = "navtest-publisher-1",
            Books =
            [
                new Book { Id = Guid.NewGuid(), Title = "top10-navtest-pub1-book-1", IsActive = true },
                new Book { Id = Guid.NewGuid(), Title = "top5-navtest-pub1-book-2", IsActive = true },
            ],
            Addr = new Shared.Scenarios.Library.Address { Zip = "12345" },
        };
        context.Publishers.Add(publisher);
        context.SaveChanges();

        try
        {
            var request = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
                HttpMethod.Get,
                resource: $"/Publishers('{publisher.Id}')?$expand=Books($filter=startswith(Title, 'top10'))",
                acceptHeader: ODataConstants.DefaultAcceptHeader,
                serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
            request.IsSuccessStatusCode.Should().BeTrue();

            var (expandedPublisher, _) = await request.DeserializeResponseAsync<Publisher>();
            expandedPublisher.Should().NotBeNull();
            expandedPublisher.Books.Should().HaveCount(1);

            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
                HttpMethod.Get,
                resource: $"/Publishers('{publisher.Id}')/Books?$filter=startswith(Title, 'top10')",
                serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
            response.IsSuccessStatusCode.Should().BeTrue();

            var (books, _) = await response.DeserializeResponseAsync<ODataV4List<Book>>();
            books.Items.Should().HaveCount(1);
        }
        finally
        {
            CleanupPublisher(context, publisher);
        }
    }

    [Fact]
    public async Task NavigationProperties_ChildrenShouldFilter_AcrossProviders()
    {
        var context = await RestierTestHelpers.GetTestableInjectedService<LibraryApi, LibraryContext>(
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());

        var publisher1 = new Publisher
        {
            Id = "navtest-publisher-1",
            Books =
            [
                new Book { Id = Guid.NewGuid(), Title = "navtest-pub1-book-1", IsActive = true },
                new Book { Id = Guid.NewGuid(), Title = "navtest-pub1-book-2", IsActive = true },
            ],
            Addr = new Shared.Scenarios.Library.Address { Zip = "12345" },
        };
        var publisher2 = new Publisher
        {
            Id = "navtest-publisher-2",
            Books =
            [
                new Book { Id = Guid.NewGuid(), Title = "navtest-pub2-book-3", IsActive = true },
            ],
            Addr = new Shared.Scenarios.Library.Address { Zip = "12345" },
        };

        context.Publishers.Add(publisher1);
        context.Publishers.Add(publisher2);
        context.SaveChanges();

        try
        {
            var request = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
                HttpMethod.Get,
                resource: $"/Publishers('{publisher1.Id}')?$expand=Books",
                acceptHeader: ODataConstants.DefaultAcceptHeader,
                serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
            request.IsSuccessStatusCode.Should().BeTrue();

            var (expandedPublisher, _) = await request.DeserializeResponseAsync<Publisher>();
            expandedPublisher.Should().NotBeNull();
            expandedPublisher.Books.Should().HaveCount(2);

            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
                HttpMethod.Get,
                resource: $"/Publishers('{publisher1.Id}')/Books",
                serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
            response.IsSuccessStatusCode.Should().BeTrue();

            var (books, _) = await response.DeserializeResponseAsync<ODataV4List<Book>>();
            books.Items.Should().HaveCount(2);
        }
        finally
        {
            CleanupPublisher(context, publisher1);
            CleanupPublisher(context, publisher2);
        }
    }

    private static void CleanupPublisher(LibraryContext context, Publisher publisher)
    {
        foreach (var book in publisher.Books.ToList())
        {
            context.Books.Remove(book);
        }

        context.Publishers.Remove(publisher);
        context.SaveChanges();
    }
}
