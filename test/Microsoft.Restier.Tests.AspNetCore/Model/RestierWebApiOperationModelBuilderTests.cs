// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Tests.Core;
using NSubstitute;
using System;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Model;

/// <summary>
/// Unit tests for the <see cref="RestierWebApiOperationModelBuilder"/> class.
/// </summary>
public class RestierWebApiOperationModelBuilderTests
{
    private readonly Type _targetApiType = typeof(SampleApi);
    private readonly IModelBuilder _innerModelBuilder = Substitute.For<IModelBuilder>();
    private readonly IModelContext _modelContext = Substitute.For<IModelContext>();

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Act
        var builder = new RestierWebApiOperationModelBuilder(_targetApiType, _innerModelBuilder);

        // Assert
        builder.Should().NotBeNull();
    }

    [Fact]
    public void GetEdmModel_ShouldReturnNull_WhenInnerModelBuilderReturnsNull()
    {
        // Arrange
        _innerModelBuilder.GetEdmModel(_modelContext).Returns((IEdmModel)null);
        var builder = new RestierWebApiOperationModelBuilder(_targetApiType, _innerModelBuilder);

        // Act
        var result = builder.GetEdmModel(_modelContext);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetEdmModel_ShouldReturnModel_WhenInnerModelBuilderReturnsValidModel()
    {
        // Arrange
        var edmModel = Substitute.For<EdmModel>();
        edmModel.DeclaredNamespaces.Returns(new[] { "TestNamespace" });
        _innerModelBuilder.GetEdmModel(_modelContext).Returns(edmModel);

        var builder = new RestierWebApiOperationModelBuilder(_targetApiType, _innerModelBuilder);

        // Act
        var result = builder.GetEdmModel(_modelContext);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<EdmModel>();
    }

    [Fact]
    public void GetEdmModel_ShouldExtendModelWithOperations()
    {
        // Arrange
        var edmModel = Substitute.For<EdmModel>();
        edmModel.DeclaredNamespaces.Returns(new[] { "TestNamespace" });
        _innerModelBuilder.GetEdmModel(_modelContext).Returns(edmModel);

        var builder = new RestierWebApiOperationModelBuilder(_targetApiType, _innerModelBuilder);

        // Act
        var result = builder.GetEdmModel(_modelContext);

        // Assert
        result.Should().NotBeNull();
        var test = edmModel.FindDeclaredOperationImports("SampleMethod");
        test.Count().Should().Be(1);
    }

    [Fact]
    public void GetEdmModel_ShouldWarnWhenBoundOperationHasNoParameters()
    {
        TestTraceListener testTraceListener = new TestTraceListener();
        Trace.Listeners.Add(testTraceListener);

        // Arrange
        var edmModel = Substitute.For<EdmModel>();
        edmModel.DeclaredNamespaces.Returns(new[] { "TestNamespace" });
        _innerModelBuilder.GetEdmModel(_modelContext).Returns(edmModel);

        var builder = new RestierWebApiOperationModelBuilder(_targetApiType, _innerModelBuilder);

        // Act
        var result = builder.GetEdmModel(_modelContext);

        // Assert
        result.Should().NotBeNull();
        // Verify that a warning is logged (if applicable).
        testTraceListener.Messages.Should().Contain("The operation 'WrongBoundMethod' was marked with [BoundOperation], but no parameters were specified to bind against.");
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
