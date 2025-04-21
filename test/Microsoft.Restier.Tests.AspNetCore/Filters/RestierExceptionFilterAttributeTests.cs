// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.OData;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.Core;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Filters;

/// <summary>
/// Unit tests for the <see cref="RestierExceptionFilterAttribute"/> class.
/// </summary>
public class RestierExceptionFilterAttributeTests
{
    private readonly RestierExceptionFilterAttribute _filter;

    public RestierExceptionFilterAttributeTests()
    {
        _filter = new RestierExceptionFilterAttribute();
    }

    [Fact]
    public async Task OnExceptionAsync_Should_Handle_ChangeSetValidationException()
    {
        // Arrange
        var context = CreateExceptionContext(new ChangeSetValidationException("Validation failed"));
        var cancellationToken = CancellationToken.None;

        // Act
        await _filter.OnExceptionAsync(context);

        // Assert
        Assert.IsType<UnprocessableEntityObjectResult>(context.Result);
    }

    [Fact]
    public async Task OnExceptionAsync_Should_Handle_CommonException()
    {
        // Arrange
        var context = CreateExceptionContext(new ODataException("OData error"));
        var cancellationToken = CancellationToken.None;

        // Act
        await _filter.OnExceptionAsync(context);

        // Assert
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task HandleChangeSetValidationException_Should_Return_True_For_ChangeSetValidationException()
    {
        // Arrange
        var context = CreateExceptionContext(new ChangeSetValidationException("Validation failed"));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await InvokePrivateMethod<Task<bool>>(
            "HandleChangeSetValidationException",
            new object[] { context, cancellationToken });

        // Assert
        Assert.True(result);
        Assert.IsType<UnprocessableEntityObjectResult>(context.Result);
    }

    [Fact]
    public async Task HandleCommonException_Should_Return_True_For_ODataException()
    {
        // Arrange
        var context = CreateExceptionContext(new ODataException("OData error"));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await InvokePrivateMethod<Task<bool>>(
            "HandleCommonException",
            new object[] { context, cancellationToken });

        // Assert
        Assert.True(result);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal((int)HttpStatusCode.BadRequest, objectResult.StatusCode);
    }

    [Fact]
    public async Task HandleCommonException_Should_Return_False_For_Null_Exception()
    {
        // Arrange
        var context = CreateExceptionContext(null);
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await InvokePrivateMethod<Task<bool>>(
            "HandleCommonException",
            new object[] { context, cancellationToken });

        // Assert
        Assert.False(result);
        Assert.Null(context.Result);
    }

    private ExceptionContext CreateExceptionContext(Exception exception)
    {
        var httpContext = Substitute.For<HttpContext>();
        var routeData = new RouteData();

        var actionContext = new ActionContext(httpContext, routeData, new ActionDescriptor(), new ModelStateDictionary());
        
        return new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = exception
        };
    }

    private T InvokePrivateMethod<T>(string methodName, object[] parameters)
    {
        var method = typeof(RestierExceptionFilterAttribute).GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Static);

        return (T)method.Invoke(null, parameters);
    }
}
