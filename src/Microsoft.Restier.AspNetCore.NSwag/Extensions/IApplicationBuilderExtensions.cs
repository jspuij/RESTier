// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.AspNetCore.NSwag;

namespace Microsoft.AspNetCore.Builder
{

    /// <summary>
    /// Extension methods on <see cref="IApplicationBuilder"/> for Restier NSwag support.
    /// </summary>
    public static class Restier_AspNetCore_NSwag_IApplicationBuilderExtensions
    {

        /// <summary>
        /// Adds middleware that serves OpenAPI 3.0 JSON for every registered Restier route at
        /// <c>/openapi/{documentName}/openapi.json</c>.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add middleware to.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> for chaining.</returns>
        public static IApplicationBuilder UseRestierOpenApi(this IApplicationBuilder app)
        {
            app.UseMiddleware<RestierOpenApiMiddleware>();
            return app;
        }

    }

}
