// <copyright file="RestierRouteBuilderExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.AspNetCore
{
    using System;
    using System.Collections.Generic;
    using Microsoft.AspNet.OData;
    using Microsoft.AspNet.OData.Extensions;
    using Microsoft.AspNet.OData.Query;
    using Microsoft.AspNet.OData.Routing.Conventions;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.OData;
    using Microsoft.Restier.AspNet;
    using Microsoft.Restier.Core;

    /// <summary>
    /// Extension methods for the <see cref="IRouteBuilder"/> interface.
    /// </summary>
    public static class RestierRouteBuilderExtensions
    {
        /// <summary>
        /// MapsRestier with the specified Route name, Route prefix and configuration action.
        /// </summary>
        /// <param name="routeBuilder">The route builder.</param>
        /// <param name="routeName">The route name.</param>
        /// <param name="routePrefix">The route prefix.</param>
        /// <param name="containerBuildAction">The container build action.</param>
        /// <param name="configureAction">The configure action.</param>
        /// <typeparam name="TApi">The api type.</typeparam>
        public static void MapRestier<TApi>(
            this IRouteBuilder routeBuilder,
            string routeName,
            string routePrefix,
            Action<IServiceCollection> containerBuildAction,
            Action<IContainerBuilder> configureAction)
        {
            Ensure.NotNull(routeBuilder, nameof(routeBuilder));
            Ensure.NotNull(routeName, nameof(routeName));
            Ensure.NotNull(routePrefix, nameof(routePrefix));

            var perRouteContainer = routeBuilder.ServiceProvider.GetRequiredService<IPerRouteContainer>();

            var oldFactory = perRouteContainer.BuilderFactory;

            Action<IContainerBuilder> extendedconfigureAction = builder =>
            {
                configureAction(builder);
                builder.AddService<IEnumerable<IODataRoutingConvention>>(OData.ServiceLifetime.Singleton, sp => routeBuilder.CreateRestierRoutingConventions(routeName));
            };

            try
            {
                perRouteContainer.BuilderFactory = () => new RestierContainerBuilder((services) =>
                     {
                         // remove the default ODataQuerySettings from OData as we will add our own.
                         services.RemoveAll<ODataQuerySettings>();

                         services
                        .AddRestierCoreServices(typeof(TApi))
                        .AddRestierConventionBasedServices(typeof(TApi));

                         containerBuildAction(services);

                         services.AddRestierDefaultServices<TApi>();
                     });

                routeBuilder.MapODataServiceRoute(routeName, routePrefix, extendedconfigureAction);
            }
            finally
            {
                perRouteContainer.BuilderFactory = oldFactory;
            }
        }

        /// <summary>
        /// Creates the default routing conventions.
        /// </summary>
        /// <param name="builder">The <see cref="IRouteBuilder"/> instance.</param>
        /// <param name="routeName">The name of the route.</param>
        /// <returns>The routing conventions created.</returns>
        private static IList<IODataRoutingConvention> CreateRestierRoutingConventions(this IRouteBuilder builder, string routeName)
        {
            var conventions = ODataRoutingConventions.CreateDefaultWithAttributeRouting(routeName, builder);
            var index = 0;
            for (; index < conventions.Count; index++)
            {
                if (conventions[index] is AttributeRoutingConvention)
                {
                    break;
                }
            }

            conventions.Insert(index + 1, new RestierRoutingConvention());
            return conventions;
        }
    }
}
