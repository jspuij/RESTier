// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="ConventionBasedAnnotationModelBuilder"/> class.
    /// </summary>
    /// <param name="apiType">The <see cref="Microsoft.Restier.Core.ApiBase"/>-derived type whose declared operations are scanned for annotation attributes. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="apiType"/> is <see langword="null"/>.</exception>
    public ConventionBasedAnnotationModelBuilder(Type apiType)
    {
        Ensure.NotNull(apiType, nameof(apiType));
        this.apiType = apiType;
    }

    /// <summary>
    /// Gets or sets the inner model builder in the chain of responsibility.
    /// </summary>
    public IModelBuilder Inner { get; set; }

    /// <inheritdoc />
    public IEdmModel GetEdmModel()
    {
        if (Inner is null)
        {
            return null;
        }

        var model = Inner.GetEdmModel() as EdmModel;
        if (model is null)
        {
            return null;
        }

        ApplyAnnotations(model);
        return model;
    }

    private static void ApplyAnnotations(EdmModel model)
    {
        foreach (var schemaType in model.SchemaElements.OfType<IEdmSchemaType>())
        {
            if (schemaType is IEdmStructuredType structuredType)
            {
                var clrType = model.GetAnnotationValue<ClrTypeAnnotation>(schemaType)?.ClrType;
                if (clrType is null)
                {
                    continue;
                }

                ApplyDescription(model, schemaType, clrType);
            }
        }
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
