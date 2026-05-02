// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Vocabularies;
using Microsoft.OData.Edm.Vocabularies.V1;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.AspNetCore.Model;

/// <summary>
/// A chained <see cref="IModelBuilder"/> that scans CLR types referenced by the
/// EDM model for .NET attributes and emits the corresponding OData vocabulary annotations.
/// </summary>
/// <remarks>
/// Runs last in the model-building chain so it can annotate every entity, complex
/// type, property, and operation contributed by inner builders. Annotations are
/// written inline so they appear on their target element in <c>$metadata</c>,
/// allowing OpenAPI generators to surface them as descriptions, computed flags,
/// and validation hints.
/// </remarks>
public class ConventionBasedAnnotationModelBuilder : IModelBuilder
{
    private readonly Type apiType;
    private readonly Dictionary<string, MethodInfo> operationMethods;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConventionBasedAnnotationModelBuilder"/> class.
    /// </summary>
    /// <param name="apiType">The <see cref="Microsoft.Restier.Core.ApiBase"/>-derived type whose declared operations are scanned for annotation attributes. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="apiType"/> is <see langword="null"/>.</exception>
    public ConventionBasedAnnotationModelBuilder(Type apiType)
    {
        Ensure.NotNull(apiType, nameof(apiType));
        this.apiType = apiType;
        this.operationMethods = BuildOperationIndex(apiType);
    }

    private static Dictionary<string, MethodInfo> BuildOperationIndex(Type apiType)
    {
        var index = new Dictionary<string, MethodInfo>(StringComparer.Ordinal);
        var methods = apiType
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Public
                      | BindingFlags.FlattenHierarchy | BindingFlags.Instance)
            .Where(m => !m.IsSpecialName && m.DeclaringType != typeof(object));

        foreach (var method in methods)
        {
            if (method.GetCustomAttribute<OperationAttribute>(inherit: true) is null)
            {
                continue;
            }

            // EDM operation name is the C# method name. The operation attributes
            // do not currently expose a Name override.
            index.TryAdd(method.Name, method);
        }

        return index;
    }

    /// <summary>
    /// Gets or sets the inner model builder in the chain of responsibility.
    /// </summary>
    public IModelBuilder Inner { get; set; }

    /// <inheritdoc />
    public IEdmModel GetEdmModel()
    {
        var inner = Inner?.GetEdmModel();
        if (inner is not EdmModel model)
        {
            // Annotation enrichment requires EdmModel APIs (AddVocabularyAnnotation).
            // If the inner model is null or a different IEdmModel implementation,
            // pass it through unchanged so the chain is preserved.
            return inner;
        }

        ApplyAnnotations(model);
        return model;
    }

    private void ApplyAnnotations(EdmModel model)
    {
        foreach (var schemaType in model.SchemaElements.OfType<IEdmSchemaType>())
        {
            if (schemaType is not IEdmStructuredType structuredType)
            {
                continue;
            }

            var clrType = model.GetAnnotationValue<ClrTypeAnnotation>(schemaType)?.ClrType;
            if (clrType is null)
            {
                continue;
            }

            ApplyDescription(model, schemaType, clrType);
            ApplyPropertyAnnotations(model, structuredType, clrType);
        }

        foreach (var operation in model.SchemaElements.OfType<IEdmOperation>())
        {
            if (!operationMethods.TryGetValue(operation.Name, out var method))
            {
                continue;
            }

            ApplyDescription(model, operation, method);
        }
    }

    private static void ApplyPropertyAnnotations(
        EdmModel model,
        IEdmStructuredType structuredType,
        Type clrType)
    {
        foreach (var edmProperty in structuredType.DeclaredProperties)
        {
            var clrProperty = clrType.GetProperty(
                edmProperty.Name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (clrProperty is null)
            {
                continue;
            }

            ApplyDescription(model, edmProperty, clrProperty);
            ApplyComputed(model, edmProperty, clrProperty);
            ApplyImmutable(model, edmProperty, clrProperty);
        }
    }

    private static void ApplyImmutable(
        EdmModel model,
        IEdmVocabularyAnnotatable target,
        PropertyInfo clrProperty)
    {
        var attr = clrProperty.GetCustomAttribute<ReadOnlyAttribute>(inherit: true);
        if (attr is null || !attr.IsReadOnly)
        {
            return;
        }

        if (HasAnnotation(model, target, CoreVocabularyModel.ImmutableTerm))
        {
            return;
        }

        var annotation = new EdmVocabularyAnnotation(
            target,
            CoreVocabularyModel.ImmutableTerm,
            new EdmBooleanConstant(true));
        annotation.SetSerializationLocation(model, EdmVocabularyAnnotationSerializationLocation.Inline);
        model.AddVocabularyAnnotation(annotation);
    }

    private static void ApplyComputed(
        EdmModel model,
        IEdmVocabularyAnnotatable target,
        PropertyInfo clrProperty)
    {
        var attr = clrProperty.GetCustomAttribute<DatabaseGeneratedAttribute>(inherit: true);
        if (attr is null || attr.DatabaseGeneratedOption == DatabaseGeneratedOption.None)
        {
            return;
        }

        if (HasAnnotation(model, target, CoreVocabularyModel.ComputedTerm))
        {
            return;
        }

        var annotation = new EdmVocabularyAnnotation(
            target,
            CoreVocabularyModel.ComputedTerm,
            new EdmBooleanConstant(true));
        annotation.SetSerializationLocation(model, EdmVocabularyAnnotationSerializationLocation.Inline);
        model.AddVocabularyAnnotation(annotation);
    }

    private static void ApplyDescription(
        EdmModel model,
        IEdmVocabularyAnnotatable target,
        MemberInfo clrMember)
    {
        var description = clrMember.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description;
        if (string.IsNullOrEmpty(description))
        {
            return;
        }

        if (HasAnnotation(model, target, CoreVocabularyModel.DescriptionTerm))
        {
            return;
        }

        var annotation = new EdmVocabularyAnnotation(
            target,
            CoreVocabularyModel.DescriptionTerm,
            new EdmStringConstant(description));
        annotation.SetSerializationLocation(model, EdmVocabularyAnnotationSerializationLocation.Inline);
        model.AddVocabularyAnnotation(annotation);
    }

    private static bool HasAnnotation(IEdmModel model, IEdmVocabularyAnnotatable target, IEdmTerm term)
    {
        return model
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(target, term.FullName())
            .Any();
    }
}
