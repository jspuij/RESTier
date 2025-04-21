// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Batch;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Batch;

/// <summary>
/// Unit tests for the <see cref="RestierChangeSetProperty"/> class.
/// </summary>
public class RestierChangeSetPropertyTests
{
    private readonly IQueryHandler queryHandler;
    private readonly IEdmModel model;
    private readonly ISubmitHandler submitHandler;
    private readonly ApiBase apiBase;

    public RestierChangeSetPropertyTests()
    {
        queryHandler = Substitute.For<IQueryHandler>();
        model = Substitute.For<IEdmModel>();
        submitHandler = Substitute.For<ISubmitHandler>();
        // Mock ApiBase
        apiBase = Substitute.For<EmptyApi>(model, queryHandler, submitHandler);
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange
        var changeSetRequestItem = new RestierBatchChangeSetRequestItem(
            apiBase,
            new[] { Substitute.For<HttpContext>() }
        );

        // Act
        var changeSetProperty = new RestierChangeSetProperty(changeSetRequestItem);

        // Assert
        Assert.NotNull(changeSetProperty.Exceptions);
        Assert.Empty(changeSetProperty.Exceptions);
        Assert.Null(changeSetProperty.ChangeSet);
    }

    [Fact]
    public async Task OnChangeSetCompleted_ShouldCompleteSuccessfully_WhenNoExceptions()
    {
        // Arrange
        var changeSetRequestItem = new RestierBatchChangeSetRequestItem(
            apiBase,
            new[] { Substitute.For<HttpContext>() }
        );
        var changeSetProperty = new RestierChangeSetProperty(changeSetRequestItem)
        {
            ChangeSet = new ChangeSet()
        };
        submitHandler.SubmitAsync(Arg.Any<SubmitContext>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new SubmitResult(changeSetProperty.ChangeSet)));

        // Act
        var task = changeSetProperty.OnChangeSetCompleted();

        // Assert
        await task;
        await submitHandler.Received(1).SubmitAsync(Arg.Any<SubmitContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnChangeSetCompleted_ShouldHandleExceptionsFromSubmitChangeSet()
    {
        // Arrange
        var changeSetRequestItem = new RestierBatchChangeSetRequestItem(
                  apiBase,
                  new[] { Substitute.For<HttpContext>() }
              );
        submitHandler.SubmitAsync(Arg.Any<SubmitContext>(), Arg.Any<CancellationToken>()).Throws((new InvalidOperationException("Test exception")));

        var changeSetProperty = new RestierChangeSetProperty(changeSetRequestItem)
        {
            ChangeSet = new ChangeSet()
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => changeSetProperty.OnChangeSetCompleted());
        Assert.Equal("Test exception", exception.Message);
    }

    public class EmptyApi : ApiBase
    {
        public EmptyApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
        {
        }
    }
}
