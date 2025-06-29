// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Routing.Conventions;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Microsoft.Restier.AspNetCore.Routing;

/// <summary>
/// Restier routing convention for <see cref="IEdmFunction"/>.
/// Get ~/entityset|singleton/function,  ~/entityset|singleton/cast/function
/// Get ~/entityset/key/function, ~/entityset/key/cast/function
/// </summary>
public class RestierFunctionRoutingConvention : RestierOperationRoutingConvention
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestierOperationRoutingConvention"/> class.
    /// </summary>
    /// <param name="modelExtender">The model extender to look up whether this EntitySet is an extended entity set or not.</param>
    public RestierFunctionRoutingConvention(RestierWebApiModelExtender modelExtender) : base(modelExtender)
    {
    }

    /// <inheritdoc />
    public override int Order => 1600;

    /// <inheritdoc />
    public override bool AppliesToAction(ODataControllerActionContext context)
    {
        base.AppliesToAction(context);

        var action = context.Action;
        var model = context.Model;
        var actionName = action.ActionName;

        StringComparison actionNameComparison = context.Options?.RouteOptions?.EnableActionNameCaseInsensitive == true
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!actionName.Equals(MethodNameOfGet, actionNameComparison))
        {
            return false;
        }

        foreach (var edmAction in model.SchemaElements.OfType<IEdmFunction>())
        {
            ProcessOperation(context, model, edmAction);
        }
        return false;
    }

    
}