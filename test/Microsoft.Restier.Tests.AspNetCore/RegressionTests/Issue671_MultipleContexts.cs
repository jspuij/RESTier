// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.Breakdance.AspNetCore;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.RegressionTests;

/// <summary>
/// Regression tests for https://github.com/OData/RESTier/issues/671.
/// </summary>
public class Issue671_MultipleContexts_SingleLibraryContext : RestierTestBase<LibraryApi>
{
    public Issue671_MultipleContexts_SingleLibraryContext()
    {
        AddRestierAction = options =>
        {
            options.AddRestierRoute<LibraryApi>(WebApiConstants.RoutePrefix, services =>
            {
                services.AddEntityFrameworkServices<LibraryContext>();
            });
        };
        TestSetup();
    }

    [Fact]
    public async Task SingleContext_LibraryApiWorks()
    {
        var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/LibraryCards");
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.IsSuccessStatusCode.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

public class Issue671_MultipleContexts_SingleMarvelContext : RestierTestBase<MarvelApi>
{
    public Issue671_MultipleContexts_SingleMarvelContext()
    {
        AddRestierAction = options =>
        {
            options.AddRestierRoute<MarvelApi>(WebApiConstants.RoutePrefix, services =>
            {
                services.AddEntityFrameworkServices<MarvelContext>();
            });
        };
        TestSetup();
    }

    [Fact]
    public async Task SingleContext_MarvelApiWorks()
    {
        var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/Characters");
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.IsSuccessStatusCode.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

public class Issue671_MultipleContexts : RestierTestBase<LibraryApi>
{
    public Issue671_MultipleContexts()
    {
        AddRestierAction = options =>
        {
            options.AddRestierRoute<LibraryApi>("Library", services =>
            {
                services.AddEntityFrameworkServices<LibraryContext>();
            });
            options.AddRestierRoute<MarvelApi>("Marvel", services =>
            {
                services.AddEntityFrameworkServices<MarvelContext>();
            });
        };
        TestSetup();
    }

    [Fact]
    public async Task MultipleContexts_ShouldQueryFirstContext()
    {
        var response = await ExecuteTestRequest(HttpMethod.Get, routePrefix: "Library", resource: "/Books?$count=true");
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("\"@odata.count\":5,");
    }

    [Fact]
    public async Task MultipleContexts_ShouldQuerySecondContext()
    {
        var response = await ExecuteTestRequest(HttpMethod.Get, routePrefix: "Marvel", resource: "/Characters?$count=true");
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("\"@odata.count\":1,");
    }
}
