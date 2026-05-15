// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using Microsoft.Restier.AspNetCore.Query;
using Microsoft.Restier.Core.Spatial;
using Microsoft.Restier.EntityFrameworkCore.Spatial;
using Microsoft.Spatial;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Query;

/// <summary>
/// Unit tests for <see cref="RestierSpatialFilterBinder"/> dispatch over geo.distance, geo.length,
/// and geo.intersects. Each test constructs a small EDM model, builds a FilterClause via
/// ODataQueryOptionParser, applies the binder, and asserts on the resulting LINQ Expression
/// tree shape. No DB, no HTTP.
/// </summary>
public class RestierSpatialFilterBinderTests
{
    // ─────────────────────────────────────────────────────────────────────
    // Tiny EDM fixtures
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EFCore-flavor entity used as the filter source. Storage type is NetTopologySuite
    /// concrete subclasses (Point, LineString), which is the case the binder must support
    /// without exact-parameter-type method lookups (Geometry.Distance(Geometry) is declared
    /// on the abstract base).
    /// </summary>
    private class NtsEntity
    {
        public int Id { get; set; }
        public NetTopologySuite.Geometries.Point Location { get; set; }
        public NetTopologySuite.Geometries.LineString RouteLine { get; set; }
    }

    /// <summary>
    /// Surrogate EDM entity. Its CLR spatial properties use Microsoft.Spatial types so that
    /// ODataConventionModelBuilder emits the correct Edm.Geometry* primitives. The property
    /// names intentionally match those on <see cref="NtsEntity"/> so that after the
    /// ClrTypeAnnotation swap the FilterBinder resolves NTS CLR members at bind time.
    /// </summary>
    private class EdmSurrogateEntity
    {
        public int Id { get; set; }
        public GeometryPoint Location { get; set; }
        public GeometryLineString RouteLine { get; set; }
    }

    /// <summary>
    /// Builds an EDM model that has correct Edm.Geometry* types (from <see cref="EdmSurrogateEntity"/>)
    /// but whose entity-type ClrTypeAnnotation is repointed to <see cref="NtsEntity"/>. This lets
    /// <see cref="QueryBinderContext"/> accept typeof(<see cref="NtsEntity"/>) while the OData parser
    /// still validates geo.length / geo.distance function signatures against the proper EDM types.
    /// </summary>
    private static (IEdmModel model, IQueryable<NtsEntity> source) BuildNtsFixture()
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntitySet<EdmSurrogateEntity>("Things");
        var model = builder.GetEdmModel();

        // Repoint the ClrTypeAnnotation so QueryBinderContext.ctor(model, settings, typeof(NtsEntity))
        // finds NtsEntity in the model and subsequent property access binds against its NTS members.
        var entityType = model.EntityContainer.FindEntitySet("Things").EntityType;
        model.SetAnnotationValue(entityType, new ClrTypeAnnotation(typeof(NtsEntity)));

        var source = new[] { new NtsEntity { Id = 1 } }.AsQueryable();
        return (model, source);
    }

    private static FilterClause ParseFilter(IEdmModel model, string entitySetName, string filterExpression)
    {
        var entitySet = model.EntityContainer.FindEntitySet(entitySetName);
        var parser = new ODataQueryOptionParser(
            model,
            entitySet.EntityType,
            entitySet,
            new Dictionary<string, string> { { "$filter", filterExpression } });
        return parser.ParseFilter();
    }

    // ─────────────────────────────────────────────────────────────────────
    // geo.length
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// geo.length(RouteLine) must lower to a MemberExpression on the storage type's "Length"
    /// property. NTS LineString inherits Length from Geometry — GetProperty walks inheritance,
    /// so this works without any reflection helper for the property case.
    /// </summary>
    [Fact]
    public void BindGeoLength_EmitsLengthPropertyAccess()
    {
        var (model, source) = BuildNtsFixture();
        var clause = ParseFilter(model, "Things", "geo.length(RouteLine) gt 0");

        var binder = new RestierSpatialFilterBinder(new ISpatialTypeConverter[] { new NtsSpatialConverter() });
        var context = new QueryBinderContext(model, new ODataQuerySettings(), typeof(NtsEntity));

        var bound = binder.ApplyBind(source, clause, context);

        // The body should be a BinaryExpression(GreaterThan, MemberExpression(prop.Length), Constant(0))
        // — but the easiest sanity check is that we got an IQueryable back without throwing.
        bound.Should().NotBeNull("the binder must successfully translate geo.length(RouteLine) gt 0");

        // Walk the expression tree looking for "Length" property access on a Geometry-derived type.
        // If we never find it, the dispatch arm wasn't reached.
        var visitor = new FindLengthAccessVisitor();
        visitor.Visit(bound.Expression);
        visitor.Found.Should().BeTrue(
            "the bound expression must contain a MemberExpression accessing the Length property of the storage type");
    }

    private class FindLengthAccessVisitor : ExpressionVisitor
    {
        public bool Found { get; private set; }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member.Name == "Length"
                && typeof(NetTopologySuite.Geometries.Geometry).IsAssignableFrom(node.Expression?.Type))
            {
                Found = true;
            }
            return base.VisitMember(node);
        }
    }
}
