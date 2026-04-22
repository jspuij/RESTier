// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EFCore;

[Collection("LibraryApiEFCore")]
public class QueryTests : QueryTests<LibraryApi, LibraryContext>
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();

    [Fact]
    public async Task NullNavigationPropertyOnExistingEntityReturns204()
    {
        // Book "Sea of Rust" (2D760F15-974D-4556-8CDF-D610128B537E) has no Publisher
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/Books(2D760F15-974D-4556-8CDF-D610128B537E)/Publisher",
            serviceCollection: ConfigureServices);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
