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
    /// Stub API class used as the <c>apiType</c> argument to the system-under-test
    /// when no operation scanning is being exercised.
    /// </summary>
    public class StubApi : ApiBase
    {
        public StubApi() : base(null, null, null) { }
    }
}

[Description("A described entity.")]
internal class DescribedEntity
{
    public int Id { get; set; }
}
