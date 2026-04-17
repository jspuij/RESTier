// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.OpenApi;
#endif
using Microsoft.OpenApi.OData;
using Microsoft.Restier.AspNetCore.Swagger;
using System;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// Extension methods on <see cref="IServiceCollection"/> for Restier Swagger support.
    /// </summary>
    public static class Restier_AspNetCore_Swagger_IServiceCollectionExtensions
    {

        /// <summary>
        /// Adds the required services to use Swagger with Restier, using the default document name.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to register Swagger services with.</param>
        /// <param name="openApiSettings">An <see cref="Action{OpenApiConvertSettings}"/> that allows you to configure the core Swagger output.</param>
        /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddRestierSwagger(this IServiceCollection services, Action<OpenApiConvertSettings> openApiSettings = null)
        {
            return services.AddRestierSwagger(RestierOpenApiDocumentGenerator.DefaultDocumentName, openApiSettings);
        }

        /// <summary>
        /// Adds the required services to use Swagger with Restier for a specific document name.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to register Swagger services with.</param>
        /// <param name="documentName">The OpenAPI document name, which maps to the Restier route prefix.</param>
        /// <param name="openApiSettings">An <see cref="Action{OpenApiConvertSettings}"/> that allows you to configure the core Swagger output.</param>
        /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddRestierSwagger(this IServiceCollection services, string documentName, Action<OpenApiConvertSettings> openApiSettings = null)
        {
            services.AddHttpContextAccessor();

            if (openApiSettings is not null)
            {
                services.AddSingleton(openApiSettings);
            }

#if NET9_0_OR_GREATER
            services.AddOpenApi(documentName, options =>
            {
                options.AddDocumentTransformer(new RestierOpenApiDocumentTransformer(openApiSettings));
            });
#endif

            return services;
        }

    }

}
