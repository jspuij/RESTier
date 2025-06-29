// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Routing;
using Microsoft.AspNetCore.OData.Routing.Conventions;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Restier.AspNetCore.Routing;

/// <summary>
/// Restier convention for <see cref="IEdmEntitySet"/>.
/// Conventions:
/// GET ~/entityset
/// GET ~/entityset/$count
/// GET ~/entityset/cast
/// GET ~/entityset/cast/$count
/// POST ~/entityset
/// POST ~/entityset/cast
/// PATCH ~/entityset ==> Delta resource set patch
/// </summary>
public class RestierEntitySetRoutingConvention : RestierRoutingConvention, IODataControllerActionConvention
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestierEntitySetRoutingConvention"/> class.
    /// </summary>
    /// <param name="modelExtender">The model extender to look up whether this EntitySet is an extended entity set or not.</param>
    public RestierEntitySetRoutingConvention(RestierWebApiModelExtender modelExtender): base(modelExtender)
    {
    }

    /// <inheritdoc />
    public int Order => 1100;

    /// <inheritdoc />
    public bool AppliesToAction(ODataControllerActionContext context)
    {
        Ensure.NotNull(context, nameof(context));

        var action = context.Action;
        var model = context.Model;

        foreach (var entitySet in model.EntityContainer.Elements.OfType<IEdmEntitySet>())
        {
            var processed = ProcessEntitySetAction(action.ActionName, entitySet, null, context, action);
        
            if (!processed)
            {
                continue;
            }

            foreach (var derivedType in model.FindAllDerivedTypes(entitySet.EntityType))
            {
                ProcessEntitySetAction(action.ActionName, entitySet, derivedType, context, action);
            }
        }

        return false;
    }

    private bool ProcessEntitySetAction(string actionName, IEdmEntitySet entitySet, IEdmStructuredType castType,
        ODataControllerActionContext context, ActionModel action)
    {
        StringComparison actionNameComparison = context.Options?.RouteOptions?.EnableActionNameCaseInsensitive == true ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        
        var isExtendedEntity = this.ExtendedEntitySetNames.Contains(entitySet.Name);

        if (actionName.Equals(MethodNameOfGet, actionNameComparison))
        {
            IEdmCollectionType castCollectionType = null;
            if (castType != null)
            {
                castCollectionType = castType.ToCollection(true);
            }

            IEdmCollectionType entityCollectionType = entitySet.EntityType.ToCollection(true);

            // GET ~/Customers or GET ~/Customers/Ns.VipCustomer
            IList<ODataSegmentTemplate> segments = new List<ODataSegmentTemplate>
            {
                new EntitySetSegmentTemplate(entitySet)
            };

            if (castType != null)
            {
                segments.Add(new CastSegmentTemplate(castCollectionType, entityCollectionType, entitySet));
            }

            ODataPathTemplate template = new ODataPathTemplate(segments);
            action.AddSelector("Get", context.Prefix, context.Model, template, context.Options?.RouteOptions);

            if (CanApplyDollarCount(entitySet, context.Options?.RouteOptions))
            {
                // GET ~/Customers/$count or GET ~/Customers/Ns.VipCustomer/$count
                segments = new List<ODataSegmentTemplate>
                {
                    new EntitySetSegmentTemplate(entitySet)
                };

                if (castType != null)
                {
                    segments.Add(new CastSegmentTemplate(castCollectionType, entityCollectionType, entitySet));
                }

                segments.Add(CountSegmentTemplate.Instance);

                template = new ODataPathTemplate(segments);
                action.AddSelector("Get", context.Prefix, context.Model, template, context.Options?.RouteOptions);
            }

            return true;
        }
        else if (actionName.Equals(MethodNameOfPost, actionNameComparison) && !isExtendedEntity)
        {
            // POST ~/Customers
            IList<ODataSegmentTemplate> segments = new List<ODataSegmentTemplate>
            {
                new EntitySetSegmentTemplate(entitySet)
            };

            if (castType != null)
            {
                IEdmCollectionType castCollectionType = castType.ToCollection(true);
                IEdmCollectionType entityCollectionType = entitySet.EntityType.ToCollection(true);
                segments.Add(new CastSegmentTemplate(castCollectionType, entityCollectionType, entitySet));
            }
            ODataPathTemplate template = new ODataPathTemplate(segments);
            action.AddSelector("Post", context.Prefix, context.Model, template, context.Options?.RouteOptions);
            return true;
        }
        else if (actionName.Equals(MethodNameOfPatch, actionNameComparison) && !isExtendedEntity)
        {
            // PATCH ~/Patch  , ~/PatchCustomers
            IList<ODataSegmentTemplate> segments = new List<ODataSegmentTemplate>
            {
                new EntitySetSegmentTemplate(entitySet)
            };

            if (castType != null)
            {
                IEdmCollectionType castCollectionType = castType.ToCollection(true);
                IEdmCollectionType entityCollectionType = entitySet.EntityType.ToCollection(true);
                segments.Add(new CastSegmentTemplate(castCollectionType, entityCollectionType, entitySet));
            }

            ODataPathTemplate template = new ODataPathTemplate(segments);
            action.AddSelector("Patch", context.Prefix, context.Model, template, context.Options?.RouteOptions);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tests whether to apply $count on the <see cref="IEdmEntitySet"/>.
    /// </summary>
    /// <param name="entitySet">The entity set to test.</param>
    /// <param name="routeOptions">The route options.</param>
    /// <returns>True/false to identify whether to apply $count.</returns>
    protected virtual bool CanApplyDollarCount(IEdmEntitySet entitySet, ODataRouteOptions routeOptions)
        => routeOptions != null ? routeOptions.EnableDollarCountRouting : false;
}