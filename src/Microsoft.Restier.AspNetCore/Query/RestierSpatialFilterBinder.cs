// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.OData;
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
/// Dispatch is added incrementally across the spec-B series: <c>geo.distance</c>,
/// <c>geo.length</c>, and <c>geo.intersects</c> are all implemented today; error-handling
/// (genus validation, no-converter diagnostics) lands in subsequent commits.
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
            case "geo.distance":
                return BindGeoDistance(node, context);
            case "geo.length":
                return BindGeoLength(node, context);
            case "geo.intersects":
                return BindGeoIntersects(node, context);
            default:
                return base.BindSingleValueFunctionCallNode(node, context);
        }
    }

    private Expression BindGeoDistance(SingleValueFunctionCallNode node, QueryBinderContext context)
    {
        return BindBinarySpatialMethod(node, context, methodName: "Distance");
    }

    private Expression BindGeoIntersects(SingleValueFunctionCallNode node, QueryBinderContext context)
    {
        return BindBinarySpatialMethod(node, context, methodName: "Intersects");
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

    /// <summary>
    /// Common dispatch for binary spatial methods (<c>Distance</c>, <c>Intersects</c>). Binds
    /// the two argument nodes, lowers any Microsoft.Spatial-valued constant into a storage
    /// value via the registered converters, and emits an
    /// <see cref="M:System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression,System.Reflection.MethodInfo,System.Linq.Expressions.Expression[])"/>
    /// using <see cref="ResolveSpatialInstanceMethod(System.Type, System.String, System.Type)"/>
    /// to find the inherited instance method on the abstract storage base type.
    /// </summary>
    private Expression BindBinarySpatialMethod(
        SingleValueFunctionCallNode node, QueryBinderContext context, string methodName)
    {
        var args = node.Parameters.ToArray();
        var bound0 = base.Bind(args[0], context);
        var bound1 = base.Bind(args[1], context);

        var lowered0 = LowerSpatialLiteralIfNeeded(node.Name, bound0, otherSideType: bound1.Type);
        var lowered1 = LowerSpatialLiteralIfNeeded(node.Name, bound1, otherSideType: bound0.Type);

        var method = ResolveSpatialInstanceMethod(lowered0.Type, methodName, lowered1.Type);
        if (method is null)
        {
            throw new ODataException(
                $"Could not resolve instance method '{methodName}' on '{lowered0.Type.FullName}' accepting '{lowered1.Type.FullName}'.");
        }

        return Expression.Call(lowered0, method, lowered1);
    }

    /// <summary>
    /// If <paramref name="bound"/> represents a Microsoft.Spatial literal (either a
    /// <see cref="ConstantExpression"/> directly holding an <c>ISpatial</c> value, or a
    /// closed expression whose static CLR type is <c>ISpatial</c> — e.g. a factory call
    /// emitted by the OData filter binder for <c>geography'…'</c> / <c>geometry'…'</c>
    /// literals), evaluate it and ask the registered converters to lower the value into a
    /// storage value of the appropriate type (inferred from the binary call's other-side
    /// argument). Returns the original expression for non-spatial inputs.
    /// </summary>
    private Expression LowerSpatialLiteralIfNeeded(
        string functionName, Expression bound, Type otherSideType)
    {
        if (!typeof(Microsoft.Spatial.ISpatial).IsAssignableFrom(bound.Type))
        {
            return bound;
        }

        object literalValue;
        if (bound is ConstantExpression ce)
        {
            literalValue = ce.Value;
        }
        else
        {
            // The OData filter binder lowers a `geography'…'` / `geometry'…'` literal into a
            // closed factory expression (e.g. GeographyFactory.Point(…).Build()), not a bare
            // ConstantExpression. Compile and invoke once so we can hand the materialized
            // Microsoft.Spatial value to the converter.
            literalValue = Expression.Lambda(bound).Compile().DynamicInvoke();
        }

        if (literalValue is not Microsoft.Spatial.ISpatial)
        {
            return bound;
        }

        // Use the abstract base storage type (e.g. NTS.Geometry) rather than a concrete
        // subtype (e.g. NTS.Point) so that the converter can materialise any geometry
        // subtype from the literal.  We probe when (a) the other side is a Microsoft.Spatial
        // type — meaning there is no storage property to guide us — or (b) the other side IS
        // a concrete storage type but a converter still handles its family (the literal may
        // carry a different subtype, e.g. geo.intersects(Point, Polygon) where the property
        // side is Point and the literal side is Polygon).
        var targetStorageType = otherSideType;
        if (typeof(Microsoft.Spatial.ISpatial).IsAssignableFrom(targetStorageType))
        {
            foreach (var c in this.converters)
            {
                var probe = ProbeStorageType(c);
                if (probe is not null)
                {
                    targetStorageType = probe;
                    break;
                }
            }
        }
        else
        {
            // Other side is already a storage type, but the literal may be a different
            // geometry subtype (e.g., Polygon when the property is Point in geo.intersects).
            // Widen to the abstract base so ToStorage can produce the correct subtype.
            foreach (var c in this.converters)
            {
                var probe = ProbeStorageType(c);
                if (probe is not null && probe.IsAssignableFrom(targetStorageType))
                {
                    targetStorageType = probe;
                    break;
                }
            }
        }

        for (var i = 0; i < this.converters.Length; i++)
        {
            if (!this.converters[i].CanConvert(targetStorageType))
            {
                continue;
            }

            try
            {
                var storageValue = this.converters[i].ToStorage(targetStorageType, literalValue);
                return Expression.Constant(storageValue, targetStorageType);
            }
            catch (InvalidOperationException ex)
            {
                throw new ODataException(ex.Message, ex);
            }
            catch (NotSupportedException ex)
            {
                throw new ODataException(ex.Message, ex);
            }
        }

        throw new ODataException(string.Format(
            Microsoft.Restier.AspNetCore.Resources.SpatialFilter_NoConverterForStorageType,
            functionName,
            "<literal>",
            targetStorageType?.FullName ?? "<unknown>"));
    }

    private static Type ProbeStorageType(ISpatialTypeConverter converter)
    {
        var ntsGeometry = Type.GetType("NetTopologySuite.Geometries.Geometry, NetTopologySuite");
        var dbGeography = Type.GetType("System.Data.Entity.Spatial.DbGeography, EntityFramework");
        var dbGeometry = Type.GetType("System.Data.Entity.Spatial.DbGeometry, EntityFramework");
        foreach (var t in new[] { ntsGeometry, dbGeography, dbGeometry })
        {
            if (t is not null && converter.CanConvert(t))
            {
                return t;
            }
        }
        return null;
    }

    /// <summary>
    /// Walks public instance methods on <paramref name="sourceType"/> and returns the first
    /// matching <paramref name="methodName"/> with arity 1 whose parameter type is assignable
    /// from <paramref name="argType"/>. Inheritance is handled implicitly because
    /// <see cref="Type.GetMethods()"/> surfaces inherited members on the derived type — so
    /// <c>Geometry.Distance(Geometry)</c> is found even when invoked against
    /// <c>typeof(Point)</c> with a <c>typeof(Point)</c> argument.
    /// </summary>
    internal static MethodInfo ResolveSpatialInstanceMethod(
        Type sourceType, string methodName, Type argType)
    {
        foreach (var m in sourceType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            if (m.Name != methodName)
            {
                continue;
            }
            var parameters = m.GetParameters();
            if (parameters.Length != 1)
            {
                continue;
            }
            if (parameters[0].ParameterType.IsAssignableFrom(argType))
            {
                return m;
            }
        }
        return null;
    }
}
