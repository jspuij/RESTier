// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Swagger;

namespace Microsoft.AspNetCore.Builder
{

    /// <summary>
    /// Extension methods on <see cref="IApplicationBuilder"/> for Restier Swagger UI support.
    /// </summary>
    public static class Restier_AspNetCore_Swagger_IApplicationBuilderExtensions
    {

        /// <summary>
        /// Adds the Swagger UI middleware for all registered Restier routes.
        /// On net8.0, also adds the middleware that serves the OpenAPI document.
        /// Call this after <c>UseEndpoints</c> where <c>MapRestierSwagger()</c> is registered.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add middleware to.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> for chaining.</returns>
        public static IApplicationBuilder UseRestierSwaggerUI(this IApplicationBuilder app)
        {
#if !NET9_0_OR_GREATER
            // On net8.0, serve the OpenAPI document via middleware since MapOpenApi() is not available.
            app.UseMiddleware<RestierOpenApiMiddleware>();
#endif

            app.UseSwaggerUI(c =>
            {
                var odataOptions = app.ApplicationServices
                    .GetRequiredService<IOptions<ODataOptions>>().Value;

                foreach (var prefix in odataOptions.GetRestierRoutePrefixes())
                {
                    var documentName = string.IsNullOrEmpty(prefix)
                        ? RestierOpenApiDocumentGenerator.DefaultDocumentName
                        : prefix;

                    c.SwaggerEndpoint($"/swagger/{documentName}/swagger.json", documentName);
                }
            });

            return app;
        }

    }

}
