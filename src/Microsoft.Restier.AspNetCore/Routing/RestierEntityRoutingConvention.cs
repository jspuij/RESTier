// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Routing;
using Microsoft.AspNetCore.OData.Routing.Conventions;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Restier.AspNetCore.Routing;

/// <summary>
/// Restier convention for <see cref="IEdmEntitySet"/> with key.
/// It supports key in parenthesis and key as segment if it's a single key.
/// Conventions:
/// GET ~/entityset/key
/// GET ~/entityset/key/cast
/// PUT ~/entityset/key
/// PUT ~/entityset/key/cast
/// PATCH ~/entityset/key
/// PATCH ~/entityset/key/cast
/// DELETE ~/entityset/key
/// DELETE ~/entityset/key/cast
/// </summary>
public class RestierEntityRoutingConvention : RestierRoutingConvention, IODataControllerActionConvention
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestierEntityRoutingConvention"/> class.
    /// </summary>
    /// <param name="modelExtender">The model extender to look up whether this EntitySet is an extended entity set or not.</param>
    public RestierEntityRoutingConvention(RestierWebApiModelExtender modelExtender) : base(modelExtender)
    {
    }

    /// <inheritdoc />
    public virtual int Order => 1300;

    /// <inheritdoc />
    public virtual bool AppliesToAction(ODataControllerActionContext context)
    {
        Ensure.NotNull(context, nameof(context));

        ActionModel action = context.Action;
        var model = context.Model;

        string actionName = action.ActionName;

        // We care about the action in this pattern: {HttpMethod}{EntityTypeName}
        (string httpMethod, string castTypeName) = Split(actionName);
        if (httpMethod == null)
        {
            return false;
        }

        foreach (var entitySet in model.EntityContainer.Elements.OfType<IEdmEntitySet>())
        {
            var isExtendedEntity = this.ExtendedEntitySetNames.Contains(entitySet.Name);
            if (isExtendedEntity && httpMethod != MethodNameOfGet)
            {
                continue;
            }

            var entityType = entitySet.EntityType;
            AddSelector(entitySet, entityType, null, context.Prefix, context.Model, action, httpMethod, context.Options?.RouteOptions);

            foreach (var derivedType in model.FindAllDerivedTypes(entitySet.EntityType))
            {
                AddSelector(entitySet, entityType, derivedType, context.Prefix, context.Model, action, httpMethod, context.Options?.RouteOptions);
            }
        }

        return false;
    }

    private (string, string) Split(string actionName)
    {
        string typeName;
        string methodName = null;
        if (actionName.StartsWith(MethodNameOfGet, StringComparison.Ordinal))
        {
            methodName = "Get";
        }
        else if (actionName.StartsWith(MethodNameOfPut, StringComparison.Ordinal))
        {
            methodName = "Put";
        }
        else if (actionName.StartsWith(MethodNameOfPatch, StringComparison.Ordinal))
        {
            methodName = "Patch";
        }
        else if (actionName.StartsWith("Delete", StringComparison.Ordinal))
        {
            methodName = "Delete";
        }

        if (methodName != null)
        {
            typeName = actionName.Substring(methodName.Length);
        }
        else
        {
            return (null, null);
        }

        if (string.IsNullOrEmpty(typeName))
        {
            return (methodName, null);
        }

        return (methodName, typeName);
    }

    private static void AddSelector(IEdmEntitySet entitySet, IEdmEntityType entityType,
        IEdmStructuredType castType, string prefix, IEdmModel model, ActionModel action, string httpMethod,
        ODataRouteOptions options)
    {
        IList<ODataSegmentTemplate> segments = new List<ODataSegmentTemplate>
        {
            new EntitySetSegmentTemplate(entitySet),
            CreateKeySegment(entityType, entitySet)
        };

        // If we have the type cast
        if (castType != null)
        {
            // ~/Customers({key})/Ns.VipCustomer
            segments.Add(new CastSegmentTemplate(castType, entityType, entitySet));
            action.AddSelector(httpMethod, prefix, model, new ODataPathTemplate(segments), options);
        }
        else
        {
            // ~/Customers({key})
            action.AddSelector(httpMethod, prefix, model, new ODataPathTemplate(segments), options);
        }
    }

}