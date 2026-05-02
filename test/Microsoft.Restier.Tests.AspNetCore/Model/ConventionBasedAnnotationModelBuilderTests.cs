// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Vocabularies;
using Microsoft.Restier.AspNetCore.Model;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Model;

public class ConventionBasedAnnotationModelBuilderTests
{
    private const string CoreDescriptionTerm = "Org.OData.Core.V1.Description";
    private const string CoreComputedTerm = "Org.OData.Core.V1.Computed";
    private const string CoreImmutableTerm = "Org.OData.Core.V1.Immutable";
    private const string ValidationMinimumTerm = "Org.OData.Validation.V1.Minimum";
    private const string ValidationMaximumTerm = "Org.OData.Validation.V1.Maximum";
    private const string ValidationPatternTerm = "Org.OData.Validation.V1.Pattern";

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

    [Fact]
    public void GetEdmModel_EmitsCoreComputed_WhenPropertyIsDatabaseGeneratedIdentity()
    {
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithIdentityKey>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        var result = sut.GetEdmModel();

        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithIdentityKey).FullName);
        var property = entityType.FindProperty(nameof(EntityWithIdentityKey.Id));
        var annotation = result
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, CoreComputedTerm)
            .Should().ContainSingle().Subject;
        ((IEdmBooleanConstantExpression)annotation.Value).Value.Should().BeTrue();
    }

    [Fact]
    public void GetEdmModel_EmitsCoreComputed_WhenPropertyIsDatabaseGeneratedComputed()
    {
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithComputedProperty>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        var result = sut.GetEdmModel();

        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithComputedProperty).FullName);
        var property = entityType.FindProperty(nameof(EntityWithComputedProperty.UpdatedAt));
        var annotation = result
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, CoreComputedTerm)
            .Should().ContainSingle().Subject;
        ((IEdmBooleanConstantExpression)annotation.Value).Value.Should().BeTrue();
    }

    [Fact]
    public void GetEdmModel_DoesNotEmitCoreComputed_WhenPropertyIsDatabaseGeneratedNone()
    {
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithNoneOption>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        var result = sut.GetEdmModel();

        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithNoneOption).FullName);
        var property = entityType.FindProperty(nameof(EntityWithNoneOption.Name));
        result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, CoreComputedTerm)
            .Should().BeEmpty();
    }

    [Fact]
    public void GetEdmModel_EmitsCoreImmutable_WhenPropertyIsReadOnlyTrue()
    {
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithReadOnlyTrue>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        var result = sut.GetEdmModel();

        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithReadOnlyTrue).FullName);
        var property = entityType.FindProperty(nameof(EntityWithReadOnlyTrue.CreatedOn));
        var annotation = result
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, CoreImmutableTerm)
            .Should().ContainSingle().Subject;
        ((IEdmBooleanConstantExpression)annotation.Value).Value.Should().BeTrue();
    }

    [Fact]
    public void GetEdmModel_DoesNotEmitCoreImmutable_WhenPropertyIsReadOnlyFalse()
    {
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithReadOnlyFalse>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        var result = sut.GetEdmModel();

        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithReadOnlyFalse).FullName);
        var property = entityType.FindProperty(nameof(EntityWithReadOnlyFalse.Notes));
        result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, CoreImmutableTerm)
            .Should().BeEmpty();
    }

    [Fact]
    public void GetEdmModel_EmitsIntegerMinMax_WhenIntPropertyHasRangeAttribute()
    {
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithIntRange>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        var result = sut.GetEdmModel();

        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithIntRange).FullName);
        var property = entityType.FindProperty(nameof(EntityWithIntRange.Score));

        var min = result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, ValidationMinimumTerm)
            .Should().ContainSingle().Subject;
        ((IEdmIntegerConstantExpression)min.Value).Value.Should().Be(0L);

        var max = result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, ValidationMaximumTerm)
            .Should().ContainSingle().Subject;
        ((IEdmIntegerConstantExpression)max.Value).Value.Should().Be(100L);
    }

    [Fact]
    public void GetEdmModel_EmitsFloatingMinMax_WhenDoublePropertyHasRangeAttribute()
    {
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithDoubleRange>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        var result = sut.GetEdmModel();

        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithDoubleRange).FullName);
        var property = entityType.FindProperty(nameof(EntityWithDoubleRange.Ratio));

        var min = result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, ValidationMinimumTerm)
            .Should().ContainSingle().Subject;
        ((IEdmFloatingConstantExpression)min.Value).Value.Should().Be(0.0);

        var max = result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, ValidationMaximumTerm)
            .Should().ContainSingle().Subject;
        ((IEdmFloatingConstantExpression)max.Value).Value.Should().Be(1.0);
    }

    [Fact]
    public void GetEdmModel_EmitsDecimalMinMax_WhenDecimalPropertyHasRangeAttribute()
    {
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithDecimalRange>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        var result = sut.GetEdmModel();

        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithDecimalRange).FullName);
        var property = entityType.FindProperty(nameof(EntityWithDecimalRange.Price));

        var min = result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, ValidationMinimumTerm)
            .Should().ContainSingle().Subject;
        ((IEdmDecimalConstantExpression)min.Value).Value.Should().Be(0.00m);

        var max = result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, ValidationMaximumTerm)
            .Should().ContainSingle().Subject;
        ((IEdmDecimalConstantExpression)max.Value).Value.Should().Be(999.99m);
    }

    [Fact]
    public void GetEdmModel_DoesNotEmitMinMax_WhenRangeAppliedToStringProperty()
    {
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithRangeOnString>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        var result = sut.GetEdmModel();

        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithRangeOnString).FullName);
        var property = entityType.FindProperty(nameof(EntityWithRangeOnString.Label));

        result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, ValidationMinimumTerm)
            .Should().BeEmpty();
        result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, ValidationMaximumTerm)
            .Should().BeEmpty();
    }

    [Fact]
    public void GetEdmModel_EmitsValidationPattern_WhenPropertyHasRegularExpression()
    {
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithRegexProperty>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        var result = sut.GetEdmModel();

        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithRegexProperty).FullName);
        var property = entityType.FindProperty(nameof(EntityWithRegexProperty.CountryCode));

        var annotation = result
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, ValidationPatternTerm)
            .Should().ContainSingle().Subject;
        ((IEdmStringConstantExpression)annotation.Value).Value.Should().Be("^[A-Z]{2}$");
    }

    [Fact]
    public void GetEdmModel_DoesNotOverrideExistingDescriptionAnnotation()
    {
        // Arrange — build the model and pre-add a Description annotation manually.
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithExistingAnnotation>();
        var entityType = inputModel.FindDeclaredType(typeof(EntityWithExistingAnnotation).FullName);
        var preExisting = new EdmVocabularyAnnotation(
            entityType,
            Microsoft.OData.Edm.Vocabularies.V1.CoreVocabularyModel.DescriptionTerm,
            new EdmStringConstant("Pre-existing."));
        preExisting.SetSerializationLocation(inputModel, EdmVocabularyAnnotationSerializationLocation.Inline);
        inputModel.AddVocabularyAnnotation(preExisting);

        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        // Act
        var result = sut.GetEdmModel();

        // Assert — the pre-existing annotation survives; no second annotation was added.
        var annotation = result
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(entityType, CoreDescriptionTerm)
            .Should().ContainSingle().Subject;
        ((IEdmStringConstantExpression)annotation.Value).Value.Should().Be("Pre-existing.");
    }

    [Fact]
    public void GetEdmModel_ReturnsNull_WhenInnerIsNull()
    {
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = null,
        };

        sut.GetEdmModel().Should().BeNull();
    }

    [Fact]
    public void GetEdmModel_ReturnsNull_WhenInnerReturnsNull()
    {
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(null),
        };

        sut.GetEdmModel().Should().BeNull();
    }

    [Fact]
    public void Constructor_Throws_WhenApiTypeIsNull()
    {
        var act = () => new ConventionBasedAnnotationModelBuilder(null);
        act.Should().Throw<ArgumentNullException>().WithParameterName("apiType");
    }

    [Fact]
    public void GetEdmModel_DoesNotEmitVocabularyAnnotation_ForMaxLengthAttribute()
    {
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithMaxLength>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        var result = sut.GetEdmModel();

        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithMaxLength).FullName);
        var property = entityType.FindProperty(nameof(EntityWithMaxLength.Code));

        // Assert — no Validation.MaxLength vocabulary annotation; structural facet remains.
        result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, "Org.OData.Validation.V1.MaxLength")
            .Should().BeEmpty();
        property.Type.AsString().MaxLength.Should().Be(13, "the structural facet should still carry the constraint");
    }
}
