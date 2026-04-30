// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OpenApi.OData;
using System;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// Extension methods on <see cref="IServiceCollection"/> for Restier NSwag support.
    /// </summary>
    public static class Restier_AspNetCore_NSwag_IServiceCollectionExtensions
    {

        /// <summary>
        /// Adds the required services to use NSwag (with ReDoc) with Restier.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to register NSwag services with.</param>
        /// <param name="openApiSettings">An <see cref="Action{OpenApiConvertSettings}"/> that allows you to configure the core OpenAPI output.</param>
        /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddRestierNSwag(this IServiceCollection services, Action<OpenApiConvertSettings> openApiSettings = null)
        {
            services.AddHttpContextAccessor();

            if (openApiSettings is not null)
            {
                services.AddSingleton(openApiSettings);
            }

            return services;
        }

    }

}
