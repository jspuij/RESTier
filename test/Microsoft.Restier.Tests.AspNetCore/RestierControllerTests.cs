// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.Breakdance.AspNetCore;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore;

/// <summary>
/// Tests for the <see cref="RestierController"/> covering basic CRUD and operation routing.
/// </summary>
public class RestierControllerTests : RestierTestBase<StoreApi>
{
    private static void di(IServiceCollection services)
    {
        services.AddTestStoreApiServices();
    }

    [Fact]
    public async Task GetTest()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/Products(1)", serviceCollection: di);
        var content = await response.Content.ReadAsStringAsync(Xunit.TestContext.Current.CancellationToken);
        TraceListener.WriteLine(content);
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task GetNonExistingEntityTest()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/Products(-1)", serviceCollection: di);
        var content = await response.Content.ReadAsStringAsync(Xunit.TestContext.Current.CancellationToken);
        TraceListener.WriteLine(content);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_WithBody_ShouldReturnCreated()
    {
        var payload = new {
            Name = "var1",
            Addr = new Address { Zip = 330 }
        };

        var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Post, resource: "/Products", payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader, serviceCollection: di);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Post_WithoutBody_ShouldReturnBadRequest()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Post, resource: "/Products",
            acceptHeader: WebApiConstants.DefaultAcceptHeader, serviceCollection: di);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().Contain("A POST requires an object to be present in the request body.");
    }

    [Fact]
    public async Task FunctionImport_NotInModel_ShouldReturnNotFound()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/GetBestProduct2", serviceCollection: di);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FunctionImport_NotInController_ShouldReturnNotImplemented()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/GetBestProduct", serviceCollection: di);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
    }

    [Fact]
    public async Task ActionImport_NotInModel_ShouldReturnNotFound()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/RemoveWorstProduct2", serviceCollection: di);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ActionImport_NotInController_ShouldReturnNotImplemented()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Post, resource: "/RemoveWorstProduct", serviceCollection: di);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        // ASP.NET Core 7.0+ Breaking change:
        // https://docs.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/7.0/mvc-empty-body-model-binding
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().Contain("Model state is not valid");
    }

    [Fact]
    public async Task GetActionImport_ShouldReturnNotFound()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/RemoveWorstProduct", serviceCollection: di);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FunctionImport_Post_WithoutBody_ShouldReturnMethodNotAllowed()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Post, resource: "/GetBestProduct", serviceCollection: di);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }
}
