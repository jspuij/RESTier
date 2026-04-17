// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Tests.Core;
using Microsoft.Restier.Tests.Shared;
using NSubstitute;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Model;

/// <summary>
/// Unit tests for the <see cref="RestierWebApiOperationModelBuilder"/> class.
/// </summary>
public class RestierWebApiOperationModelBuilderTests
{
    private readonly Type _targetApiType = typeof(SampleApi);
    private readonly IModelBuilder _innerModelBuilder = Substitute.For<IModelBuilder>();

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange
        var extender = new RestierWebApiModelExtender(_targetApiType);

        // Act
        var builder = new RestierWebApiOperationModelBuilder(_targetApiType, extender);

        // Assert
        builder.Should().NotBeNull();
    }

    [Fact]
    public void GetEdmModel_ShouldReturnNull_WhenInnerModelBuilderReturnsNull()
    {
        // Arrange
        _innerModelBuilder.GetEdmModel().Returns((IEdmModel)null);
        var extender = new RestierWebApiModelExtender(_targetApiType);
        var builder = new RestierWebApiOperationModelBuilder(_targetApiType, extender)
        {
            Inner = _innerModelBuilder
        };

        // Act
        var result = builder.GetEdmModel();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetEdmModel_ShouldReturnModel_WhenInnerModelBuilderReturnsValidModel()
    {
        // Arrange
        var edmModel = new EdmModel();
        var container = new EdmEntityContainer("TestNamespace", "DefaultContainer");
        edmModel.AddElement(container);
        _innerModelBuilder.GetEdmModel().Returns(edmModel);

        var extender = new RestierWebApiModelExtender(_targetApiType);
        var builder = new RestierWebApiOperationModelBuilder(_targetApiType, extender)
        {
            Inner = _innerModelBuilder
        };

        // Act
        var result = builder.GetEdmModel();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<EdmModel>();
    }

    [Fact]
    public void GetEdmModel_ShouldExtendModelWithOperations()
    {
        // Arrange
        var edmModel = new EdmModel();
        var container = new EdmEntityContainer("TestNamespace", "DefaultContainer");
        edmModel.AddElement(container);
        _innerModelBuilder.GetEdmModel().Returns(edmModel);

        var extender = new RestierWebApiModelExtender(_targetApiType);
        var builder = new RestierWebApiOperationModelBuilder(_targetApiType, extender)
        {
            Inner = _innerModelBuilder
        };

        // Act
        var result = builder.GetEdmModel();

        // Assert
        result.Should().NotBeNull();
        var test = edmModel.FindDeclaredOperationImports("SampleMethod");
        test.Count().Should().Be(1);
    }

    [Fact]
    public void GetEdmModel_ShouldWarnWhenBoundOperationHasNoParameters()
    {
        var testTraceListener = new TestTraceListener();
        Trace.Listeners.Add(testTraceListener);

        try
        {
            // Arrange
            var edmModel = new EdmModel();
            var container = new EdmEntityContainer("TestNamespace", "DefaultContainer");
            edmModel.AddElement(container);
            _innerModelBuilder.GetEdmModel().Returns(edmModel);

            var extender = new RestierWebApiModelExtender(_targetApiType);
            var builder = new RestierWebApiOperationModelBuilder(_targetApiType, extender)
            {
                Inner = _innerModelBuilder
            };

            // Act
            var result = builder.GetEdmModel();

            // Assert
            result.Should().NotBeNull();
            testTraceListener.Messages.Should().Contain("The operation 'WrongBoundMethod' was marked with [BoundOperation], but no parameters were specified to bind against.");
        }
        finally
        {
            Trace.Listeners.Remove(testTraceListener);
        }
    }
}

// Sample API class for testing purposes
public class SampleApi
{
    [UnboundOperation]
    public int SampleMethod()
    {
        return 42;
    }

    [BoundOperation]
    public int WrongBoundMethod()
    {
        return 42;
    }
}
