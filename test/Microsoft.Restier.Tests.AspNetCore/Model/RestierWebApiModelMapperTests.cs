// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using NSubstitute;
using System;
using System.Linq;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Model;

/// <summary>
/// Unit tests for <see cref="RestierWebApiModelMapper"/>.
/// </summary>
public class RestierWebApiModelMapperTests
{
    [Fact]
    public void TryGetRelevantType_ShouldReturnTrue_WhenEntitySetIsFound()
    {
        // Arrange
        var mockInnerMapper = Substitute.For<IModelMapper>();
        var mockModel = Substitute.For<IEdmModel>();
        var mockEntityContainer = Substitute.For<IEdmEntityContainer>();
        var mockEntitySet = Substitute.For<IEdmEntitySet>();
        var mockEntityType = Substitute.For<IEdmEntityType>();
        var mockAnnotation = new ClrTypeAnnotation(typeof(string));

        mockModel.EntityContainer.Returns(mockEntityContainer);
        mockEntityContainer.Elements.Returns(new[] { mockEntitySet });
        mockEntitySet.Name.Returns("TestEntitySet");
        mockEntitySet.Type.Returns(new EdmCollectionType(new EdmEntityTypeReference(mockEntityType, false)));
        mockModel.GetAnnotationValue<ClrTypeAnnotation>(mockEntityType).Returns(mockAnnotation);
        var mockApi = Substitute.For<ApiBase>(mockModel, Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>());

        var context = new InvocationContext(mockApi);
        var mapper = new RestierWebApiModelMapper { Inner = mockInnerMapper };

        // Act
        var result = mapper.TryGetRelevantType(context, "TestEntitySet", out var relevantType);

        // Assert
        result.Should().BeTrue();
        relevantType.Should().Be(typeof(string));
    }

    [Fact]
    public void TryGetRelevantType_ShouldReturnFalse_WhenEntitySetIsNotFound()
    {
        // Arrange
        var mockInnerMapper = Substitute.For<IModelMapper>();
        var mockApi = Substitute.For<ApiBase>(Substitute.For<IEdmModel>(), Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>());
        var mockModel = Substitute.For<IEdmModel>();
        var mockEntityContainer = Substitute.For<IEdmEntityContainer>();

        mockModel.EntityContainer.Returns(mockEntityContainer);
        mockEntityContainer.Elements.Returns(Enumerable.Empty<IEdmEntityContainerElement>());

        var context = new InvocationContext(mockApi);
        var mapper = new RestierWebApiModelMapper { Inner = mockInnerMapper };

        // Act
        var result = mapper.TryGetRelevantType(context, "NonExistentEntitySet", out var relevantType);

        // Assert
        result.Should().BeFalse();
        relevantType.Should().BeNull();
    }

    [Fact]
    public void TryGetRelevantType_ShouldDelegateToInnerMapper_WhenElementIsNotFound()
    {
        // Arrange
        var mockInnerMapper = Substitute.For<IModelMapper>();
        var mockApi = Substitute.For<ApiBase>(Substitute.For<IEdmModel>(), Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>());
        var mockModel = Substitute.For<IEdmModel>();
        var mockEntityContainer = Substitute.For<IEdmEntityContainer>();

        mockModel.EntityContainer.Returns(mockEntityContainer);
        mockEntityContainer.Elements.Returns(Enumerable.Empty<IEdmEntityContainerElement>());

        var context = new InvocationContext(mockApi);
        var mapper = new RestierWebApiModelMapper { Inner = mockInnerMapper };

        Type expectedType = typeof(int);
        mockInnerMapper.TryGetRelevantType(context, "NonExistentEntitySet", out Arg.Any<Type>())
            .Returns(x =>
            {
                x[2] = expectedType;
                return true;
            });

        // Act
        var result = mapper.TryGetRelevantType(context, "NonExistentEntitySet", out var relevantType);

        // Assert
        result.Should().BeTrue();
        relevantType.Should().Be(expectedType);
    }

    [Fact]
    public void TryGetRelevantType_ComposableFunction_ShouldDelegateToInnerMapper()
    {
        // Arrange
        var mockInnerMapper = Substitute.For<IModelMapper>();
        var context = Substitute.For<InvocationContext>(Substitute.For<ApiBase>(Substitute.For<IEdmModel>(), Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>()));
        var mapper = new RestierWebApiModelMapper { Inner = mockInnerMapper };

        Type expectedType = typeof(int);
        mockInnerMapper.TryGetRelevantType(context, "Namespace", "FunctionName", out Arg.Any<Type>())
            .Returns(x =>
            {
                x[3] = expectedType;
                return true;
            });

        // Act
        var result = mapper.TryGetRelevantType(context, "Namespace", "FunctionName", out var relevantType);

        // Assert
        result.Should().BeTrue();
        relevantType.Should().Be(expectedType);
    }
}
