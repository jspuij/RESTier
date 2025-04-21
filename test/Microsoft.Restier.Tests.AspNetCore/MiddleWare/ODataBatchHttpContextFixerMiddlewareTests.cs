// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Restier.AspNetCore.Middleware;
using NSubstitute;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Middleware;

/// <summary>
/// Unit tests for <see cref="ODataBatchHttpContextFixerMiddleware"/>.
/// </summary>
public class ODataBatchHttpContextFixerMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldSetHttpContext_WhenHttpContextIsNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var contextAccessor = Substitute.For<IHttpContextAccessor>();
        contextAccessor.HttpContext = null;
        var requestDelegate = Substitute.For<RequestDelegate>();
        var middleware = new ODataBatchHttpContextFixerMiddleware(requestDelegate);

        // Act
        await middleware.InvokeAsync(httpContext, contextAccessor);

        // Assert
        contextAccessor.HttpContext.Should().Be(httpContext);
        await requestDelegate.Received(1).Invoke(httpContext);
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotOverrideHttpContext_WhenHttpContextIsNotNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var existingHttpContext = new DefaultHttpContext();
        var contextAccessor = Substitute.For<IHttpContextAccessor>();
        contextAccessor.HttpContext = existingHttpContext;
        var requestDelegate = Substitute.For<RequestDelegate>();
        var middleware = new ODataBatchHttpContextFixerMiddleware(requestDelegate);

        // Act
        await middleware.InvokeAsync(httpContext, contextAccessor);

        // Assert
        contextAccessor.HttpContext.Should().Be(existingHttpContext);
        await requestDelegate.Received(1).Invoke(httpContext);
    }
}
