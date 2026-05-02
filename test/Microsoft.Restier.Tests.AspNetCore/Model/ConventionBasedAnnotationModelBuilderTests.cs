// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Vocabularies;
using Microsoft.Restier.AspNetCore.Model;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Model;

public class ConventionBasedAnnotationModelBuilderTests
{
    private const string CoreDescriptionTerm = "Org.OData.Core.V1.Description";

    [Fact]
    public void GetEdmModel_EmitsCoreDescription_WhenEntityTypeHasDescriptionAttribute()
    {
        // Arrange
        var inputModel = AnnotationTestFixtures.BuildModelWith<DescribedEntity>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        // Act
        var result = sut.GetEdmModel();

        // Assert
        var entityType = result.FindDeclaredType(typeof(DescribedEntity).FullName);
        entityType.Should().NotBeNull("the input model should still contain DescribedEntity");

        var annotation = result
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(entityType, CoreDescriptionTerm)
            .Should().ContainSingle().Subject;

        var stringValue = annotation.Value.Should().BeAssignableTo<IEdmStringConstantExpression>().Subject;
        stringValue.Value.Should().Be("A described entity.");
    }

    [Fact]
    public void GetEdmModel_EmitsCoreDescription_WhenPropertyHasDescriptionAttribute()
    {
        // Arrange
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithDescribedProperty>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        // Act
        var result = sut.GetEdmModel();

        // Assert
        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithDescribedProperty).FullName);
        var property = entityType.FindProperty(nameof(EntityWithDescribedProperty.Name));

        var annotation = result
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, CoreDescriptionTerm)
            .Should().ContainSingle().Subject;

        ((IEdmStringConstantExpression)annotation.Value).Value.Should().Be("The display name of the entity.");
    }

    [Fact]
    public void GetEdmModel_EmitsCoreDescription_WhenComplexTypeHasDescriptionAttribute()
    {
        // Arrange
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithComplexProperty>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        // Act
        var result = sut.GetEdmModel();

        // Assert
        var complexType = result.FindDeclaredType(typeof(DescribedComplex).FullName);
        complexType.Should().BeAssignableTo<IEdmComplexType>("ODataConventionModelBuilder should infer DescribedComplex as a complex type");

        var annotation = result
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(complexType, CoreDescriptionTerm)
            .Should().ContainSingle().Subject;

        ((IEdmStringConstantExpression)annotation.Value).Value.Should().Be("A postal address.");
    }

    [Fact]
    public void GetEdmModel_EmitsCoreDescription_WhenOperationMethodHasDescriptionAttribute()
    {
        // Arrange
        var inputModel = AnnotationTestFixtures.BuildModelWithUnboundFunction(
            namespaceName: "Microsoft.Restier.Tests.AspNetCore.Model",
            functionName: nameof(ApiWithDescribedOperation.CountActive));
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(ApiWithDescribedOperation))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        // Act
        var result = sut.GetEdmModel();

        // Assert
        var operation = result.SchemaElements.OfType<IEdmOperation>().Single();
        var annotation = result
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(operation, CoreDescriptionTerm)
            .Should().ContainSingle().Subject;

        ((IEdmStringConstantExpression)annotation.Value).Value.Should().Be("Returns the active record count.");
    }
}
