// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.OData.Routing.Conventions;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Restier.AspNetCore.Routing;

/// <summary>
/// Base class for Restier routing conventions.
/// </summary>
public abstract class RestierRoutingConvention
{
    /// <summary>
    /// The name of the Restier controller, which is used to route requests to the appropriate controller.
    /// </summary>
    protected const string RestierControllerName = "Restier";

    /// <summary>
    /// The names of the Get Method that are used to handle requests in Restier controllers.
    /// </summary>
    protected const string MethodNameOfGet = "Get";

    /// <summary>
    /// The names of the Post Method that are used to handle requests in Restier controllers.
    /// </summary>
    protected const string MethodNameOfPost = "Post";

    /// <summary>
    /// The names of the Put Method that are used to handle requests in Restier controllers.
    /// </summary>
    protected const string MethodNameOfPut = "Put";

    /// <summary>
    /// The names of the Patch Method that are used to handle requests in Restier controllers.
    /// </summary>
    protected const string MethodNameOfPatch = "Patch";

    /// <summary>
    /// The names of the Delete Method that are used to handle requests in Restier controllers.
    /// </summary>
    protected const string MethodNameOfDelete = "Delete";

    /// <summary>
    /// The names of the PostAction Method that are used to handle requests in Restier controllers.
    /// </summary>
    protected const string MethodNameOfPostAction = "PostAction";

    /// <summary>
    /// Initializes a new instance of the <see cref="RestierEntitySetRoutingConvention"/> class.
    /// </summary>
    /// <param name="modelExtender">The model extender to look up whether this EntitySet is an extended entity set or not.</param>
    public RestierRoutingConvention(RestierWebApiModelExtender modelExtender)
    {
        Ensure.NotNull(modelExtender, nameof(modelExtender));
        ExtendedEntitySetNames = modelExtender.EntitySetProperties.Select(x => x.Name).ToHashSet();
    }

    /// <summary>
    /// A hashset of extended EntitySet names that are used to determine if an EntitySet is an extended entity set.
    /// </summary>
    protected HashSet<string> ExtendedEntitySetNames { get; }

    /// <inheritdoc />
    public virtual bool AppliesToController(ODataControllerActionContext context)
    {
        var controllerNameComparison = context.Options?.RouteOptions?.EnableActionNameCaseInsensitive == true ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return string.Equals(context.Controller.ControllerName, RestierControllerName, controllerNameComparison);
    }

    /// <summary>
    /// Creates a key segment template for the specified entity type and navigation source.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="navigationSource">The navigation source.</param>
    /// <param name="keyPrefix">The key prefix.</param>
    /// <returns></returns>
    protected static KeySegmentTemplate CreateKeySegment(IEdmEntityType entityType,
        IEdmNavigationSource navigationSource, string keyPrefix = "key")
    {
        Ensure.NotNull(entityType, nameof(entityType));

        IDictionary<string, string> keyTemplates = new Dictionary<string, string>();
        var keys = entityType.Key().ToArray();
        if (keys.Length == 1)
        {
            // Id={key}
            keyTemplates[keys[0].Name] = $"{{{keyPrefix}}}";
        }
        else
        {
            // Id1={keyId1},Id2={keyId2}
            foreach (var key in keys)
            {
                keyTemplates[key.Name] = $"{{{keyPrefix}{key.Name}}}";
            }
        }

        return new KeySegmentTemplate(keyTemplates, entityType, navigationSource);
    }
}