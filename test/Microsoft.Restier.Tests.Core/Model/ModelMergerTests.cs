// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Vocabularies;
using Microsoft.Restier.Core.Model;
using NSubstitute;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Restier.Tests.Core.Model;

public class ModelMergerTests
{
    [Fact]
    public void Merge_Should_Add_SchemaElements_Except_EntityContainer()
    {
        // Arrange
        var sourceModel = new EdmModel();
        var targetModel = new EdmModel();

        var entityType = Substitute.For<IEdmSchemaType>();
        entityType.SchemaElementKind.Returns(EdmSchemaElementKind.TypeDefinition);

        sourceModel.AddElement(new EdmEntityContainer("bla","blabla"));
        sourceModel.AddElement(entityType);

        // Act
        ModelMerger.Merge(sourceModel, targetModel);

        // Assert
        targetModel.SchemaElements.Should().ContainSingle().Which.Should().Be(entityType);
    }

    [Fact]
    public void Merge_Should_Add_VocabularyAnnotations()
    {
        // Arrange
        var sourceModel = new EdmModel();
        var targetModel = new EdmModel();

        var annotation = Substitute.For<IEdmVocabularyAnnotation>();
        sourceModel.AddVocabularyAnnotation(annotation);

        // Act
        ModelMerger.Merge(sourceModel, targetModel);

        // Assert
        targetModel.VocabularyAnnotations.Should().ContainSingle().Which.Should().Be(annotation);
    }

    [Fact]
    public void Merge_Should_Add_EntitySets_If_Not_Exists()
    {
        // Arrange
        var sourceModel = Substitute.For<IEdmModel>();
        var targetModel = new EdmModel();

        var sourceContainer = new EdmEntityContainer("NS", "SourceContainer");
        var targetContainer = new EdmEntityContainer("NS", "TargetContainer");
        targetModel.AddElement(targetContainer);

        var entityType = new EdmEntityType("NS", "Entity");
        var entitySet = sourceContainer.AddEntitySet("Entities", entityType);

        sourceModel.EntityContainer.Returns(sourceContainer);

        sourceModel.SchemaElements.Returns(new IEdmSchemaElement[0]);
        sourceModel.VocabularyAnnotations.Returns(new IEdmVocabularyAnnotation[0]);

        // Act
        ModelMerger.Merge(sourceModel, targetModel);

        // Assert
        targetContainer.FindEntitySet("Entities").Should().NotBeNull();
    }

    [Fact]
    public void Merge_Should_Add_Singletons_If_Not_Exists()
    {
        // Arrange
        var sourceModel = Substitute.For<IEdmModel>();
        var targetModel = new EdmModel();

        var sourceContainer = new EdmEntityContainer("NS", "SourceContainer");
        var targetContainer = new EdmEntityContainer("NS", "TargetContainer");
        targetModel.AddElement(targetContainer);

        var entityType = new EdmEntityType("NS", "Entity");
        var singleton = sourceContainer.AddSingleton("Single", entityType);

        sourceModel.EntityContainer.Returns(sourceContainer);

        sourceModel.SchemaElements.Returns(new IEdmSchemaElement[0]);
        sourceModel.VocabularyAnnotations.Returns(new IEdmVocabularyAnnotation[0]);

        // Act
        ModelMerger.Merge(sourceModel, targetModel);

        // Assert
        targetContainer.FindSingleton("Single").Should().NotBeNull();
    }

    [Fact]
    public void Merge_Should_Add_OperationImports_If_Not_Exists()
    {
        // Arrange
        var sourceModel = Substitute.For<IEdmModel>();
        var targetModel = new EdmModel();

        var sourceContainer = new EdmEntityContainer("NS", "SourceContainer");
        var targetContainer = new EdmEntityContainer("NS", "TargetContainer");
        targetModel.AddElement(targetContainer);

        var function = new EdmFunction("NS", "Func", EdmCoreModel.Instance.GetInt32(false));
        var functionImport = sourceContainer.AddFunctionImport("Func", function);

        sourceModel.EntityContainer.Returns(sourceContainer);
        
        sourceModel.SchemaElements.Returns(new IEdmSchemaElement[0]);
        sourceModel.VocabularyAnnotations.Returns(new IEdmVocabularyAnnotation[0]);

        // Act
        ModelMerger.Merge(sourceModel, targetModel);

        // Assert
        targetContainer.FindOperationImports("Func").Should().NotBeNull();
    }

    [Fact]
    public void Merge_Should_Return_If_SourceEntityContainer_Is_Null()
    {
        // Arrange
        var sourceModel = Substitute.For<IEdmModel>();
        var targetModel = Substitute.For<EdmModel>();

        sourceModel.EntityContainer.Returns((IEdmEntityContainer)null);

        // Act
        var act = () => ModelMerger.Merge(sourceModel, targetModel);
        act.Should().NotThrow();

    }
}