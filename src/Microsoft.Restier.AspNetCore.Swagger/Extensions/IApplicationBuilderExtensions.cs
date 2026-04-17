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
    /// Extension methods on <see cref="IApplicationBuilder"/> for Restier Swagger support.
    /// </summary>
    public static class Restier_AspNetCore_Swagger_IApplicationBuilderExtensions
    {

        /// <summary>
        /// Adds middleware to serve OpenAPI documents and the Swagger UI for all registered Restier routes.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add middleware to.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> for chaining.</returns>
        public static IApplicationBuilder UseRestierSwaggerUI(this IApplicationBuilder app)
        {
            app.UseMiddleware<RestierOpenApiMiddleware>();

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
