// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Routing.Conventions;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Microsoft.Restier.AspNetCore.Routing;

/// <summary>
/// Restier routing convention for <see cref="IEdmOperationImport"/>.
/// Get ~/functionimport(....)
/// Post ~/actionimport
/// </summary>
public class RestierOperationImportRoutingConvention: RestierOperationRoutingConvention
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestierOperationRoutingConvention"/> class.
    /// </summary>
    /// <param name="modelExtender">The model extender to look up whether this EntitySet is an extended entity set or not.</param>
    public RestierOperationImportRoutingConvention(RestierWebApiModelExtender modelExtender) : base(modelExtender)
    {
    }

    /// <inheritdoc />
    public override int Order => 1900;

    /// <inheritdoc />
    public override bool AppliesToAction(ODataControllerActionContext context)
    {
        var action = context.Action;
        var model = context.Model;
        var actionName = action.ActionName;

        StringComparison actionNameComparison = context.Options?.RouteOptions?.EnableActionNameCaseInsensitive == true ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        foreach (var edmOperationImport in model.EntityContainer.Elements.OfType<IEdmOperationImport>())
        {
            if (edmOperationImport is IEdmActionImport actionImport && actionName.Equals(MethodNameOfPostAction, actionNameComparison))
            {
                IEdmEntitySetBase targetEntitySet;
                actionImport.TryGetStaticEntitySet(model, out targetEntitySet);

                ODataPathTemplate template = new ODataPathTemplate(new ActionImportSegmentTemplate(actionImport, targetEntitySet));
                action.AddSelector("Post", context.Prefix, context.Model, template, context.Options?.RouteOptions);
            }
            else if (edmOperationImport is IEdmFunctionImport functionImport && actionName.Equals(MethodNameOfGet, actionNameComparison))
            {
                IEdmEntitySetBase targetSet;
                functionImport.TryGetStaticEntitySet(model, out targetSet);

                // TODO: 
                // 1) shall we check the [HttpGet] attribute, or does the ASP.NET Core have the default?
                ODataPathTemplate template = new ODataPathTemplate(new FunctionImportSegmentTemplate(functionImport, targetSet));
                action.AddSelector("Get", context.Prefix, context.Model, template, context.Options?.RouteOptions);
            }
        }
        return false;
    }
}