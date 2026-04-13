// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Routing.Conventions;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;

namespace Microsoft.Restier.AspNetCore.Routing;

/// <summary>
/// Restier Conventions for <see cref="IEdmAction"/> and <see cref="IEdmFunction"/>.
/// Get ~/entityset|singleton/function,  ~/entityset|singleton/cast/function
/// Get ~/entityset/key/function, ~/entityset/key/cast/function
/// Post ~/entityset|singleton/action,  ~/entityset|singleton/cast/action
/// Post ~/entityset/key/action,  ~/entityset/key/cast/action
/// </summary>
public abstract class RestierOperationRoutingConvention : RestierRoutingConvention, IODataControllerActionConvention
{
    private Dictionary<IEdmEntityType, IEnumerable<IEdmEntitySet>> collections;
    private Dictionary<IEdmEntityType, IEnumerable<IEdmSingleton>> singletons;

    /// <summary>
    /// Initializes a new instance of the <see cref="RestierOperationRoutingConvention"/> class.
    /// </summary>
    /// <param name="modelExtender">The model extender to look up whether this EntitySet is an extended entity set or not.</param>
    public RestierOperationRoutingConvention(RestierWebApiModelExtender modelExtender) : base(modelExtender)
    {
       
    }

    /// <inheritdoc />
    public abstract int Order { get; }

    /// <inheritdoc />
    public virtual bool AppliesToAction(ODataControllerActionContext context)
    {
       var model = context.Model;
       collections = model.EntityContainer.Elements.OfType<IEdmEntitySet>()
           .GroupBy(g => g.EntityType)
           .ToDictionary(g => g.Key, g => g.Select(x => x));

        singletons = model.EntityContainer.Elements.OfType<IEdmSingleton>()
            .GroupBy(g => g.EntityType)
            .ToDictionary(g => g.Key, g => g.Select(x => x));
        return false;
    }

    /// <summary>
    /// Process the operation for the given action context and model.
    /// </summary>
    /// <param name="context">The controller action context that contains information about the controller and the action that will process this route.</param>
    /// <param name="model">The EDM Model that is applicable to this route.</param>
    /// <param name="edmOperation">The operation to process.</param>
    protected void ProcessOperation(ODataControllerActionContext context, IEdmModel model, IEdmOperation edmOperation)
    {
        if (!edmOperation.IsBound)
        {
            return;
        }

        IEdmOperationParameter bindingParameter = edmOperation.Parameters.FirstOrDefault();
        if (bindingParameter == null)
        {
            // bound operation at least has one parameter which type is the binding type.
            return;
        }

        IEdmTypeReference bindingType = bindingParameter.Type;

        if (bindingType.TypeKind() == EdmTypeKind.Collection)
        {
            var collectionType = (IEdmCollectionType)bindingType.Definition;
            var entityType = collectionType.ElementType.Definition as IEdmEntityType;

            if (entityType == null)
            {
                return;
            }

            if (!collections.TryGetValue(entityType, out var matchingCollections))
            {
                return;
            }

            foreach (var collection in matchingCollections)
            {
                context.NavigationSource = collection;

                AddSelector(context, edmOperation, false, entityType, collection, null);

                foreach (var derivedType in model.FindAllDerivedTypes(entityType))
                {
                    AddSelector(context, edmOperation, false, entityType, collection, derivedType);
                }
            }
        }
        else if (bindingType.TypeKind() == EdmTypeKind.Entity)
        {
            var entityType = (IEdmEntityType)bindingType.Definition;

            if (entityType == null)
            {
                return;
            }

            if (collections.TryGetValue(entityType, out var matchingCollections))
            {
                foreach (var collection in matchingCollections)
                {
                    context.NavigationSource = collection;

                    AddSelector(context, edmOperation, true, entityType, collection, null);

                    foreach (var derivedType in model.FindAllDerivedTypes(entityType))
                    {
                        AddSelector(context, edmOperation, true, entityType, collection, derivedType);
                    }
                }
            }
            if (singletons.TryGetValue(entityType, out var matchingSingletons))
            {
                foreach (var singleton in matchingSingletons)
                {
                    context.NavigationSource = singleton;

                    AddSelector(context, edmOperation, false, entityType, singleton, null);

                    foreach (var derivedType in model.FindAllDerivedTypes(entityType))
                    {
                        AddSelector(context, edmOperation, false, entityType, singleton, derivedType);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Add the template to the action
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="edmOperation">The Edm operation.</param>
    /// <param name="hasKeyParameter">Has key parameter or not.</param>
    /// <param name="entityType">The entity type.</param>
    /// <param name="navigationSource">The navigation source.</param>
    /// <param name="castType">The type cast.</param>
    protected static void AddSelector(ODataControllerActionContext context,
        IEdmOperation edmOperation,
        bool hasKeyParameter,
        IEdmEntityType entityType,
        IEdmNavigationSource navigationSource,
        IEdmStructuredType castType)
    {
        Ensure.NotNull(context, nameof(context));
        Ensure.NotNull(edmOperation, nameof(edmOperation));

        // Now, let's add the selector model.
        IList<ODataSegmentTemplate> segments = new List<ODataSegmentTemplate>();
        if (context.EntitySet != null)
        {
            segments.Add(new EntitySetSegmentTemplate(context.EntitySet));
            if (hasKeyParameter)
            {
                segments.Add(CreateKeySegment(entityType, navigationSource));
            }
        }
        else if (context.Singleton != null)
        {
            segments.Add(new SingletonSegmentTemplate(context.Singleton));
        }

        if (castType != null)
        {
            if (context.Singleton != null || !hasKeyParameter)
            {
                segments.Add(new CastSegmentTemplate(castType, entityType, navigationSource));
            }
            else
            {
                segments.Add(new CastSegmentTemplate(new EdmCollectionType(castType.ToEdmTypeReference(false)),
                    new EdmCollectionType(entityType.ToEdmTypeReference(false)), navigationSource));
            }
        }

        IEdmNavigationSource targetEntitySet = null;
        if (edmOperation.GetReturn() != null)
        {
            targetEntitySet = edmOperation.GetTargetEntitySet(navigationSource, context.Model);
        }

        string httpMethod;
        if (edmOperation.IsAction())
        {
            if (edmOperation.IsBound)
            {
                segments.Add(new ActionSegmentTemplate((IEdmAction)edmOperation, targetEntitySet));
            }
            else
            {
                segments.Add(new ActionSegmentTemplate(new OperationSegment(edmOperation, null)));
            }
            httpMethod = "Post";
        }
        else
        {
            IDictionary<string, string> parameters = GetFunctionParameters(edmOperation);
            segments.Add(new FunctionSegmentTemplate(parameters, (IEdmFunction)edmOperation, targetEntitySet));
            httpMethod = "Get";
        }

        ODataPathTemplate template = new ODataPathTemplate(segments);
        context.Action.AddSelector(httpMethod, context.Prefix, context.Model, template, context.Options?.RouteOptions);
    }

    private static IDictionary<string, string> GetFunctionParameters(IEdmOperation operation)
    {
        Ensure.NotNull(operation, nameof(operation));
        Contract.Assert(operation.IsFunction());

        IDictionary<string, string> parameters = new Dictionary<string, string>();

        // we can allow the action has other parameters except the function parameters.
        foreach (var parameter in operation.IsBound ? operation.Parameters.Skip(1) : operation.Parameters)
        {
            parameters[parameter.Name] = $"{{{parameter.Name}}}";
        }

        return parameters;
    }
}