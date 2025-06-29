// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Routing.Conventions;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using System;
using System.Linq;

namespace Microsoft.Restier.AspNetCore.Routing;

/// <summary>
/// Restier convention for <see cref="IEdmSingleton"/>.
/// The Conventions:
/// Get|Put|Patch ~/singleton
/// Get|Put|Patch ~/singleton/cast
/// </summary>
public class RestierSingletonRoutingConvention : RestierRoutingConvention, IODataControllerActionConvention
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestierSingletonRoutingConvention"/> class.
    /// </summary>
    /// <param name="modelExtender">The model extender to look up whether this EntitySet is an extended entity set or not.</param>
    public RestierSingletonRoutingConvention(RestierWebApiModelExtender modelExtender) : base(modelExtender)
    {
    }

    /// <inheritdoc />
    public virtual int Order => 1200;

    /// <inheritdoc />
    public bool AppliesToAction(ODataControllerActionContext context)
    {
        Ensure.NotNull(context, nameof(context));

        var action = context.Action;
        var model = context.Model;

        foreach (var singleton in model.EntityContainer.Elements.OfType<IEdmSingleton>())
        {
            ProcessSingletonAction(action.ActionName, singleton, null, context, action);

            foreach (var derivedType in model.FindAllDerivedTypes(singleton.EntityType))
            {
                ProcessSingletonAction(action.ActionName, singleton, derivedType, context, action);
            }
        }

        return false;
    }

    private void ProcessSingletonAction(
        string actionMethodName,
        IEdmSingleton singleton,
        IEdmStructuredType castType,
        ODataControllerActionContext context, ActionModel action)
    {
        string singletonName = singleton.Name;

        if (!IsSupportedActionName(context, actionMethodName, out string httpMethod))
        {
            return;
        }

        if (castType == null)
        {
            // ~/Me
            ODataPathTemplate template = new ODataPathTemplate(new SingletonSegmentTemplate(singleton));
            action.AddSelector(httpMethod, context.Prefix, context.Model, template, context.Options?.RouteOptions);
        }
        else
        {
            IEdmEntityType entityType = singleton.EntityType;

            // ~/Me/Namespace.TypeCast
            ODataPathTemplate template = new ODataPathTemplate(
                new SingletonSegmentTemplate(singleton),
                new CastSegmentTemplate(castType, entityType, singleton));

            action.AddSelector(httpMethod, context.Prefix, context.Model, template, context.Options?.RouteOptions);
        }

    }

    private static bool IsSupportedActionName(ODataControllerActionContext context, string actionName, out string httpMethod)
    {
        StringComparison actionNameComparison = context.Options?.RouteOptions?.EnableActionNameCaseInsensitive == true ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (actionName.Equals(MethodNameOfGet, actionNameComparison))
        {
            httpMethod = "Get";
            return true;
        }

        httpMethod = "";
        return false;
    }
}
