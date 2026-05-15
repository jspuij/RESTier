// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.OData.UriParser;
using Microsoft.Restier.Core.Spatial;

namespace Microsoft.Restier.AspNetCore.Query;

/// <summary>
/// <see cref="T:Microsoft.AspNetCore.OData.Query.Expressions.FilterBinder"/> subclass that translates the three OData v4-core spatial
/// functions (<c>geo.distance</c>, <c>geo.length</c>, <c>geo.intersects</c>) into LINQ
/// method/property access against the storage CLR type so EF6 and EF Core can translate
/// them to native SQL spatial operators. Anything else falls through to the base
/// <see cref="T:Microsoft.AspNetCore.OData.Query.Expressions.FilterBinder"/> behavior.
/// </summary>
/// <remarks>
/// Dispatch is added incrementally across the spec-B series: <c>geo.length</c> is implemented
/// today; <c>geo.distance</c> and <c>geo.intersects</c> arms land in subsequent commits and
/// currently fall through to the base implementation.
/// </remarks>
public class RestierSpatialFilterBinder : FilterBinder
{
    private readonly ISpatialTypeConverter[] converters;

    /// <summary>
    /// Initializes a new instance of the <see cref="RestierSpatialFilterBinder"/> class.
    /// </summary>
    /// <param name="converters">
    /// The <see cref="ISpatialTypeConverter"/> instances registered in the route service
    /// container. Used by the <c>geo.distance</c> and <c>geo.intersects</c> arms to lower
    /// Microsoft.Spatial literals into storage values; <c>geo.length</c> does not need a
    /// converter (pure property access). May be null or empty when no converter is registered.
    /// </param>
    public RestierSpatialFilterBinder(IEnumerable<ISpatialTypeConverter> converters = null)
    {
        this.converters = converters?.ToArray() ?? Array.Empty<ISpatialTypeConverter>();
    }

    /// <inheritdoc />
    public override Expression BindSingleValueFunctionCallNode(
        SingleValueFunctionCallNode node, QueryBinderContext context)
    {
        switch (node.Name)
        {
            case "geo.length":
                return BindGeoLength(node, context);
            default:
                return base.BindSingleValueFunctionCallNode(node, context);
        }
    }

    private Expression BindGeoLength(SingleValueFunctionCallNode node, QueryBinderContext context)
    {
        // geo.length is unary: a single LineString-typed argument.
        var args = node.Parameters.ToArray();
        var bound = base.Bind(args[0], context);

        // Geometry.Length (NTS) and DbGeography.Length / DbGeometry.Length (EF6) are all
        // instance properties. GetProperty walks inheritance, so a concrete LineString-typed
        // expression still finds the inherited Length on Geometry.
        return Expression.Property(bound, "Length");
    }
}
