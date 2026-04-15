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
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

[Collection("LibraryApi")]
public class ValidationTests : RestierTestBase<LibraryApi>
{
    [Fact]
    public async Task Validation_StringLengthExceeded()
    {
        var bookRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/Books?$top=1",
            acceptHeader: ODataConstants.MinimalAcceptHeader,
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
        bookRequest.IsSuccessStatusCode.Should().BeTrue();

        var (bookList, errorContent) = await bookRequest.DeserializeResponseAsync<ODataV4List<Book>>();

        bookList.Should().NotBeNull();
        bookList.Items.Should().NotBeNullOrEmpty();
        errorContent.Should().BeNullOrEmpty();

        var book = bookList.Items.First();
        book.Should().NotBeNull();

        book.Isbn = "This is a really really long string.";

        var bookEditResponse = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Put,
            resource: $"/Books({book.Id})",
            payload: book,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
        var content = await TraceListener.LogAndReturnMessageContentAsync(bookEditResponse);

        bookEditResponse.IsSuccessStatusCode.Should().BeFalse();
        content.Should().Contain("validationentries");
        content.Should().Contain("MaxLengthAttribute");
    }
}
