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
/// Restier routing convention for <see cref="IEdmAction"/>.
/// Post ~/entityset|singleton/action,  ~/entityset|singleton/cast/action
/// Post ~/entityset/key/action,  ~/entityset/key/cast/action
/// </summary>
public class RestierActionRoutingConvention : RestierOperationRoutingConvention
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestierOperationRoutingConvention"/> class.
    /// </summary>
    /// <param name="modelExtender">The model extender to look up whether this EntitySet is an extended entity set or not.</param>
    public RestierActionRoutingConvention(RestierWebApiModelExtender modelExtender) : base(modelExtender)
    {
    }

    /// <inheritdoc />
    public override int Order => 1700;

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

        if (!actionName.Equals(MethodNameOfPostAction, actionNameComparison))
        {
            return false;
        }

        foreach (var edmAction in model.SchemaElements.OfType<IEdmAction>())
        {
            ProcessOperation(context, model, edmAction);
        }
        return false;
    }

    
}