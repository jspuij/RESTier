// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.ComponentModel;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.Tests.AspNetCore.Model;

/// <summary>
/// Helpers and fixture types used by <c>ConventionBasedAnnotationModelBuilderTests</c>.
/// </summary>
internal static class AnnotationTestFixtures
{
    /// <summary>
    /// Builds an <see cref="EdmModel"/> from a single CLR entity type via
    /// <see cref="ODataConventionModelBuilder"/>, which sets <c>ClrTypeAnnotation</c>
    /// on the resulting EDM types.
    /// </summary>
    public static EdmModel BuildModelWith<T>() where T : class
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntityType<T>();
        return (EdmModel)builder.GetEdmModel();
    }

    /// <summary>
    /// Inner builder that returns a fixed model. Used to feed a known input model
    /// into the system-under-test without invoking the real RESTier chain.
    /// </summary>
    public sealed class StaticInnerBuilder : IModelBuilder
    {
        private readonly IEdmModel model;

        public StaticInnerBuilder(IEdmModel model) => this.model = model;

        public IModelBuilder Inner { get; set; }

        public IEdmModel GetEdmModel() => model;
    }

    /// <summary>
    /// Stub API class used as the <c>apiType</c> argument to the system-under-test.
    /// Only the type metadata (via <c>typeof(StubApi)</c>) is consumed by the builder;
    /// the constructor is never invoked at runtime by the tests, so the <see langword="null"/>
    /// arguments to <see cref="Microsoft.Restier.Core.ApiBase"/> are safe in practice.
    /// </summary>
    public class StubApi : ApiBase
    {
        // Constructor is never executed; only typeof(StubApi) is used by the operation index.
        public StubApi() : base(null, null, null) { }
    }
}

[Description("A described entity.")]
internal class DescribedEntity
{
    public int Id { get; set; }
}

internal class EntityWithDescribedProperty
{
    public int Id { get; set; }

    [System.ComponentModel.Description("The display name of the entity.")]
    public string Name { get; set; }
}

[System.ComponentModel.Description("A postal address.")]
internal class DescribedComplex
{
    public string Street { get; set; }

    public string Zip { get; set; }
}

internal class EntityWithComplexProperty
{
    public int Id { get; set; }

    public DescribedComplex Address { get; set; }
}
