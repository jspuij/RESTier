// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Routing.Conventions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace Microsoft.Restier.AspNetCore
{

    /// <summary>
    /// The default routing convention implementation.
    /// </summary>
    public class RestierRoutingConvention : IODataControllerActionConvention
    {
        private const string RestierControllerName = "Restier";
        private const string MethodNameOfGet = "Get";
        private const string MethodNameOfPost = "Post";
        private const string MethodNameOfPut = "Put";
        private const string MethodNameOfPatch = "Patch";
        private const string MethodNameOfDelete = "Delete";
        private const string MethodNameOfPostAction = "PostAction";

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierRoutingConvention"/> class.
        /// </summary>
        /// <param name="order">The order of the routing convention.</param>
        public RestierRoutingConvention(int order)
        {
            Order = order;
        }

        /// <inheritdoc />
        public int Order { get; }

        /*

                /// <summary>
                /// Selects the appropriate action based on the parsed OData URI.
                /// </summary>
                /// <param name="routeContext">The route context.</param>
                /// <returns>An enumerable of ControllerActionDescriptors.</returns>
                public IEnumerable<ControllerActionDescriptor> SelectAction(RouteContext routeContext)
                {
                    
                }

                private bool TryFindMatchingODataActions(RouteContext context, out IEnumerable<ControllerActionDescriptor> actions)
                {
                    var routingConventions = context.HttpContext.Request.GetRoutingConventions();
                    if (routingConventions is not null)
                    {
                        foreach (var convention in routingConventions)
                        {
                            if (convention != this)
                            {
                                var actionDescriptor = convention.SelectAction(context);
                                if (actionDescriptor?.Any() == true)
                                {
                                    actions = actionDescriptor;
                                    return true;
                                }
                            }
                        }
                    }

                    actions = null;
                    return false;
                }

                private static bool IsMetadataPath(ODataPath odataPath)
                {
                    return odataPath.PathTemplate == "~" || odataPath.PathTemplate == "~/$metadata";
                }

                private static bool IsAction(ODataPathSegment lastSegment)
                {
                    if (lastSegment is OperationSegment operationSeg)
                    {
                        if (operationSeg.Operations.FirstOrDefault() is IEdmAction)
                        {
                            return true;
                        }
                    }

                    if (lastSegment is OperationImportSegment operationImportSeg)
                    {
                        if (operationImportSeg.OperationImports.FirstOrDefault() is IEdmActionImport)
                        {
                            return true;
                        }
                    }

                    return false;
                } */

        /// <inheritdoc />
        public bool AppliesToController(ODataControllerActionContext context)
        {
            Ensure.NotNull(context, nameof(context));
            var controller = context.Controller;
            var model = context.Model;
            return string.Equals(controller.ControllerName, RestierControllerName, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public bool AppliesToAction(ODataControllerActionContext context)
        {
            Ensure.NotNull(context, nameof(context));
            var controller = context.Controller;
            var action = context.Action;
            var model = context.Model;

            action.AddSelector("Get",  "api/tests", model, new ODataPathTemplate(new EntitySetSegmentTemplate(model.FindDeclaredEntitySet("Books"))), null);

            return string.Equals(action.ActionName, MethodNameOfDelete, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(action.ActionName, MethodNameOfGet, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(action.ActionName, MethodNameOfPatch, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(action.ActionName, MethodNameOfPost, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(action.ActionName, MethodNameOfPostAction, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(action.ActionName, MethodNameOfPut, StringComparison.OrdinalIgnoreCase);
        }
    }

}
