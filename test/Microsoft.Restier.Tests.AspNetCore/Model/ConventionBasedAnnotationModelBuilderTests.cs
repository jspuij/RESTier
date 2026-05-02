// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

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
}
