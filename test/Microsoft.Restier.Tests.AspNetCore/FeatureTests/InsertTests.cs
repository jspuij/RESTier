// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using CloudNimble.Breakdance.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

[Collection("LibraryApi")]
public class InsertTests : RestierTestBase<LibraryApi>
{
    [Fact]
    public async Task InsertBook()
    {
        var book = new Book
        {
            Title = "Inserting Yourself into Every Situation",
            Isbn = "0118006345789",
        };

        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Post,
            resource: "/Publishers('Publisher1')/Books",
            payload: book,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());

        response.Should().NotBeNull();

        var createdBookResult = await response.DeserializeResponseAsync<Book>();
        var createdBook = createdBookResult.Response;

        response.IsSuccessStatusCode.Should().BeTrue();
        createdBook.Should().NotBeNull();
        createdBook.Id.Should().NotBeEmpty();
    }
}
