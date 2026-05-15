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
public class RestierSpatialFilterBinder : FilterBinder
{
    private readonly ISpatialTypeConverter[] converters;

    /// <summary>
    /// Initializes a new instance of the <see cref="RestierSpatialFilterBinder"/> class.
    /// </summary>
    /// <param name="converters">
    /// The <see cref="ISpatialTypeConverter"/> instances registered in the route service
    /// container. May be null or empty, in which case the binder falls through to the base
    /// behavior for every <c>geo.*</c> call.
    /// </param>
    public RestierSpatialFilterBinder(IEnumerable<ISpatialTypeConverter> converters = null)
    {
        this.converters = converters?.ToArray() ?? Array.Empty<ISpatialTypeConverter>();
    }

    /// <inheritdoc />
    public override Expression BindSingleValueFunctionCallNode(
        SingleValueFunctionCallNode node, QueryBinderContext context)
    {
        // Subsequent tasks fill in the three dispatch arms. Today every call falls through.
        return base.BindSingleValueFunctionCallNode(node, context);
    }
}
