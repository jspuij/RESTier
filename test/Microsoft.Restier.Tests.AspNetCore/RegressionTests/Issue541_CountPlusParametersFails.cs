// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.Breakdance.AspNetCore;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.RegressionTests;

/// <summary>
/// Regression tests for https://github.com/OData/RESTier/issues/541.
/// </summary>
public class Issue541_CountPlusParametersFails : RestierTestBase<LibraryApi>
{
    public Issue541_CountPlusParametersFails()
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
    public async Task CountShouldntThrowExceptions()
    {
        var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/Readers?$count=true");
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        content.Should().Contain("\"@odata.count\":2,");
    }

    [Fact]
    public async Task CountPlusTopShouldntThrowExceptions()
    {
        var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/Readers?$top=5&$count=true");
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        content.Should().Contain("\"@odata.count\":2,");
    }

    [Fact]
    public async Task CountPlusTopPlusFilterShouldntThrowExceptions()
    {
        var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/Readers?$top=5&$count=true&$filter=FullName eq 'p1'");
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        content.Should().Contain("\"@odata.count\":1,");
    }

    [Fact]
    public async Task CountPlusTopPlusProjectionShouldntThrowExceptions()
    {
        var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/Readers?$top=5&$count=true&$select=Id,FullName");
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        content.Should().Contain("\"@odata.count\":2,");
    }

    [Fact]
    public async Task CountPlusSelectShouldntThrowExceptions()
    {
        var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/Readers?$count=true&$select=Id,FullName");
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        content.Should().Contain("\"@odata.count\":2,");
    }

    [Fact]
    public async Task CountPlusExpandShouldntThrowExceptions()
    {
        var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/Publishers?$top=5&$count=true&$expand=Books");
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        content.Should().Contain("\"@odata.count\":2,");
    }
}
