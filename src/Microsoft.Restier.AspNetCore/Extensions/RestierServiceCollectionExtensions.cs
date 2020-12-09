// <copyright file="RestierServiceCollectionExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.AspNetCore
{
    using System;
    using Microsoft.AspNet.OData.Extensions;
    using Microsoft.AspNet.OData.Formatter.Deserialization;
    using Microsoft.AspNet.OData.Formatter.Serialization;
    using Microsoft.AspNet.OData.Interfaces;
    using Microsoft.AspNet.OData.Query;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.OData;
    using Microsoft.Restier.AspNetCore.Formatter;
    using Microsoft.Restier.AspNetCore.Model;
    using Microsoft.Restier.AspNetCore.Operation;
    using Microsoft.Restier.AspNetCore.Query;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Model;
    using Microsoft.Restier.Core.Operation;
    using Microsoft.Restier.Core.Query;

    /// <summary>
    /// Contains extension methods of <see cref="IServiceCollection"/>.
    /// This method is used to add odata publisher service into container.
    /// </summary>
    public static class RestierServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Restier and OData Services to the Service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>An <see cref="IODataBuilder"/> that can be used to further configure the OData services.</returns>
        public static IODataBuilder AddRestier(this IServiceCollection services)
        {
            Ensure.NotNull(services, nameof(services));

            var odataBuilder = services.AddOData();

            return odataBuilder;
        }

        /// <summary>
        /// This method is used to add odata publisher service into container.
        /// </summary>
        /// <typeparam name="T">The Api type.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>Current <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddRestierDefaultServices<T>(this IServiceCollection services)
        {
            Ensure.NotNull(services, nameof(services));

            if (services.HasService<DefaultRestierServicesDetectionDummy>())
            {
                // Avoid applying multiple times to a same service collection.
                return services;
            }

            services.AddSingleton<DefaultRestierServicesDetectionDummy>();

            // Do not add Restier implementation of chained service inside the container twice.
            if (!services.HasService<RestierModelBuilder>())
            {
                services.AddChainedService<IModelBuilder, RestierModelBuilder>();
            }

            // Do not add Restier implementation of chained service inside the container twice.
            if (!services.HasService<RestierModelExtender>())
            {
                AddRestierModelExtender(services, typeof(T));
            }

            // Do not add Restier implementation of chained service inside the container twice.
            if (!services.HasService<RestierOperationModelBuilder>())
            {
                AddOperationModelBuilder(services, typeof(T));
            }

            // Only add if none are there. We have removed the default OData one before.
            services.TryAddScoped((sp) => new ODataQuerySettings
            {
                HandleNullPropagation = HandleNullPropagationOption.False,
                PageSize = null,  // no support for server enforced PageSize, yet
            });

            // default registration, same as OData. Should not be neccesary but just in case.
            services.TryAddSingleton<ODataValidationSettings>();

            // OData already registers the ODataSerializerProvider, so if we have 2, either the developer
            // added one, or we already did. OData resolves the right one so multiple can be registered.
            if (services.HasServiceCount<ODataSerializerProvider>() < 2)
            {
                services.AddSingleton<ODataSerializerProvider, DefaultRestierSerializerProvider>();
            }

            // OData already registers the ODataDeserializerProvider, so if we have 2, either the developer
            // added one, or we already did. OData resolves the right one so multiple can be registered.
            if (services.HasServiceCount<ODataDeserializerProvider>() < 2)
            {
                services.AddSingleton<ODataDeserializerProvider, DefaultRestierDeserializerProvider>();
            }

            // TryAdd only adds if no other implementation is already registered.
            services.TryAddSingleton<IOperationExecutor, RestierOperationExecutor>();

            // OData already registers the ODataPayloadValueConverter, so if we have 2, either the developer
            // added one, or we already did. OData resolves the right one so multiple can be registered.
            if (services.HasServiceCount<ODataPayloadValueConverter>() < 2)
            {
                services.AddSingleton<ODataPayloadValueConverter, RestierPayloadValueConverter>();
            }

            // Do not add Restier implementation of chained service inside the container twice.
            if (!services.HasService<RestierModelMapper>())
            {
                services.AddChainedService<IModelMapper, RestierModelMapper>();
            }

            services.TryAddScoped<RestierQueryExecutorOptions>();

            // Do not add Restier implementation of chained service inside the container twice.
            if (!services.HasService<RestierQueryExecutor>())
            {
                services.AddChainedService<IQueryExecutor, RestierQueryExecutor>();
            }

            return services;
        }

        /// <summary>
        /// Adds the Model extender for the target type to the services collection.
        /// </summary>
        /// <param name="services">The services collection.</param>
        /// <param name="targetType">The target type.</param>
        internal static void AddRestierModelExtender(IServiceCollection services, Type targetType)
        {
            Ensure.NotNull(services, nameof(services));
            Ensure.NotNull(targetType, nameof(targetType));

            // The model builder must maintain a singleton life time, for holding states and being injected into
            // some other services.
            services.AddSingleton(new RestierModelExtender(targetType));

            services.AddChainedService<IModelBuilder, RestierModelExtender.ModelBuilder>();
            services.AddChainedService<IModelMapper, RestierModelExtender.ModelMapper>();
            services.AddChainedService<IQueryExpressionExpander, RestierModelExtender.QueryExpressionExpander>();
            services.AddChainedService<IQueryExpressionSourcer, RestierModelExtender.QueryExpressionSourcer>();
        }

        /// <summary>
        /// Adds the modelbuilder to the services collection for the target type.
        /// </summary>
        /// <param name="services">The services collection.</param>
        /// <param name="targetType">The target type.</param>
        internal static void AddOperationModelBuilder(IServiceCollection services, Type targetType)
        {
            services.AddChainedService<IModelBuilder>((sp, next) => new RestierOperationModelBuilder(targetType, next));
        }

        /// <summary>
        /// Dummy class to detect double registration of Default restier services inside a container.
        /// </summary>
        private sealed class DefaultRestierServicesDetectionDummy
        {
        }
    }
}