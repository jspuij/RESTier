// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Batch;
using Microsoft.AspNetCore.OData.Formatter.Deserialization;
using Microsoft.AspNetCore.OData.Formatter.Serialization;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Batch;
using Microsoft.Restier.AspNetCore.Formatter;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.AspNetCore.Operation;
using Microsoft.Restier.AspNetCore.Query;
using Microsoft.Restier.AspNetCore.Routing;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Operation;
using Microsoft.Restier.Core.Query;
using System;

namespace Microsoft.Restier.AspNetCore;

/// <summary>
/// Extension Methods on <see cref="ODataOptions"/> for Restier.
/// </summary>
public static class RestierODataOptionsExtensions
{
    /// <summary>
    /// Adds a Restier route for the specified API type to the OData options.
    /// </summary>
    /// <typeparam name="TApi">The type of the API to add.</typeparam>
    /// <param name="oDataOptions">The <see cref="ODataOptions"/> to add a route to.</param>
    /// <param name="configureRouteServices">Action to configure the Restier Route services.</param>
    /// <param name="useRestierBatching">Use the default Restier Batching Handler</param>
    /// <returns>The <see cref="ODataOptions"/>.</returns>
    public static ODataOptions AddRestierRoute<TApi>
    (this ODataOptions oDataOptions,
            Action<IServiceCollection> configureRouteServices, bool useRestierBatching = true)
    where TApi : ApiBase
        => oDataOptions.AddRestierRoute<TApi>(string.Empty, configureRouteServices, useRestierBatching);

    /// <summary>
    /// Adds a Restier route for the specified API type to the OData options.
    /// </summary>
    /// <typeparam name="TApi">The type of the API to add.</typeparam>
    /// <param name="oDataOptions">The <see cref="ODataOptions"/> to add a route to.</param>
    /// <param name="routePrefix">The route prefix to use.</param>
    /// <param name="configureRouteServices">Action to configure the Restier Route services.</param>
    /// <param name="useRestierBatching">Use the default Restier Batching Handler</param>
    /// <returns>The <see cref="ODataOptions"/>.</returns>
    public static ODataOptions AddRestierRoute<TApi>(
        this ODataOptions oDataOptions,
        string routePrefix,
        Action<IServiceCollection> configureRouteServices,
        bool useRestierBatching = true)
    where TApi : ApiBase
    => AddRestierRoute(oDataOptions, typeof(TApi), routePrefix , configureRouteServices, useRestierBatching);


    private static ODataOptions AddRestierRoute(
        ODataOptions oDataOptions,
        Type type, string routePrefix,
        Action<IServiceCollection> configureRouteServices,
        bool useRestierBatching)
    {
        Ensure.NotNull(oDataOptions, nameof(oDataOptions));
        Ensure.NotNull(type, nameof(type));
        Ensure.NotNull(routePrefix, nameof(routePrefix));

        // Restier does not support qualified operation calls.
        oDataOptions.RouteOptions.EnableQualifiedOperationCall = false;

        // We have to do some trickery here. The model building process in OData is now separate from the route building process,
        // but Restier is not really expecting that. So we have to build the model first and then add the model and the model extender
        // to the route services. That also means that we have to invoke the service configuring action twice: once for the model building container
        // and once for the route container.
        // It might make sense to redesign the model builder to 
        var modelBuildingServices = new ServiceCollection();
        modelBuildingServices.TryAddSingleton<IChainOfResponsibilityFactory<IModelBuilder>, DefaultChainOfResponsibilityFactory<IModelBuilder>>();
        modelBuildingServices.TryAddSingleton<ModelMerger>();
        configureRouteServices.Invoke(modelBuildingServices);
        modelBuildingServices.AddSingleton< IChainedService<IModelBuilder>, RestierWebApiModelBuilder>()
            .AddSingleton(new RestierWebApiModelExtender(type))
            .AddSingleton<IChainedService<IModelBuilder>>(sp => new RestierWebApiOperationModelBuilder(type, sp.GetRequiredService<RestierWebApiModelExtender>()));

        IEdmModel model;
        RestierWebApiModelExtender modelExtender;
        ServiceProvider modelBuildingServiceProvider = null;

        try
        {
            modelBuildingServiceProvider = modelBuildingServices.BuildServiceProvider();
            var modelBuilderFactory = modelBuildingServiceProvider
                .GetRequiredService<IChainOfResponsibilityFactory<IModelBuilder>>();
            var modelBuilder = modelBuilderFactory.Create();
            model = modelBuilder.GetEdmModel();
            modelExtender = modelBuildingServiceProvider.GetRequiredService<RestierWebApiModelExtender>();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Model building failed with exception {exception.Message}", exception);
        }
        finally
        {
            modelBuildingServiceProvider?.Dispose();
        }

//        var extType = Type.GetType("Microsoft.AspNetCore.OData.Edm.EdmModelExtensions, Microsoft.AspNetCore.OData");
//;
//        var method = extType.GetMethod("ResolveNavigationSource", BindingFlags.Static | BindingFlags.Public, new[] { typeof(IEdmModel), typeof(string), typeof(bool) });
//        method.Invoke(null, [model, "Test", true]);

        oDataOptions.AddRouteComponents(routePrefix, model, services =>
        {
            //RWM: Add the API as the specific API type first, then if an ApiBase instance is requested from the container,
            //     get the existing instance.
            services
                .AddScoped(type, type)
                .AddScoped(sp => (ApiBase)sp.GetService(type));

            services.RemoveAll<ODataQuerySettings>()
                .AddRestierCoreServices()
                .AddRestierConventionBasedServices(type);

            configureRouteServices.Invoke(services);

            services.AddSingleton<IChainedService<IModelBuilder>, RestierWebApiModelBuilder>()
                .AddSingleton(modelExtender)
                .AddSingleton<IChainedService<IModelBuilder>>(sp => new RestierWebApiOperationModelBuilder(type, sp.GetRequiredService<RestierWebApiModelExtender>()))
                .AddSingleton<IChainedService<IModelMapper>, RestierWebApiModelMapper>()
                .AddSingleton<IChainedService<IQueryExpressionExpander>, RestierQueryExpressionExpander>()
                .AddSingleton<IChainedService<IQueryExpressionSourcer>, RestierQueryExpressionSourcer>();

            // Only add if none are there. We have removed the default OData one before.
            services.TryAddScoped((sp) => new ODataQuerySettings
            {
                HandleNullPropagation = HandleNullPropagationOption.False,
                PageSize = null,  // no support for server enforced PageSize, yet
            });

            // default registration, same as OData. Should not be necesary but just in case.
            services.TryAddSingleton<ODataValidationSettings>();

            // OData already registers the ODataSerializerProvider, so if we have 2, either the developer
            // added one, or we already did. OData resolves the right one so multiple can be registered.
            if (services.HasServiceCount<IODataSerializerProvider>() < 2)
            {
                services.AddSingleton<IODataSerializerProvider, DefaultRestierSerializerProvider>();
            }

            // OData already registers the ODataDeserializerProvider, so if we have 2, either the developer
            // added one, or we already did. OData resolves the right one so multiple can be registered.
            if (services.HasServiceCount<IODataSerializerProvider>() < 2)
            {
                services.AddSingleton<IODataDeserializerProvider, DefaultRestierDeserializerProvider>();
            }

            services.TryAddSingleton<IOperationExecutor, RestierOperationExecutor>();

            // OData already registers the ODataPayloadValueConverter, so if we have 2, either the developer
            // added one, or we already did. OData resolves the right one so multiple can be registered.
            if (services.HasServiceCount<ODataPayloadValueConverter>() < 2)
            {
                services.AddSingleton<ODataPayloadValueConverter, RestierPayloadValueConverter>();
            }

            services.AddSingleton<IChainedService<IModelMapper>, RestierModelMapper>();
            services.AddSingleton<IChainedService<IQueryExecutor>, RestierQueryExecutor>();

            if (useRestierBatching)
            {
                services.AddSingleton<ODataBatchHandler>(sp => new RestierBatchHandler()
                {
                    PrefixName = routePrefix,
                });
            }
        });

        // Add the Restier routing conventions to the OData options.
        oDataOptions.Conventions.Add(new RestierActionRoutingConvention(modelExtender));
        oDataOptions.Conventions.Add(new RestierEntitySetRoutingConvention(modelExtender));
        oDataOptions.Conventions.Add(new RestierEntityRoutingConvention(modelExtender));
        oDataOptions.Conventions.Add(new RestierFunctionRoutingConvention(modelExtender));
        oDataOptions.Conventions.Add(new RestierOperationImportRoutingConvention(modelExtender));
        oDataOptions.Conventions.Add(new RestierSingletonRoutingConvention(modelExtender));

        return oDataOptions;
    }
}