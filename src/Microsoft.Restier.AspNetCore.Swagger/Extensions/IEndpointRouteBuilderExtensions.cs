// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder
{

    /// <summary>
    /// Extension methods on <see cref="IEndpointRouteBuilder"/> for Restier Swagger support.
    /// </summary>
    public static class Restier_AspNetCore_Swagger_IEndpointRouteBuilderExtensions
    {

        /// <summary>
        /// Maps the OpenAPI document endpoints for all registered Restier routes.
        /// On net8.0 this is a no-op; the document is served by middleware instead.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add endpoints to.</param>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> for chaining.</returns>
        public static IEndpointRouteBuilder MapRestierSwagger(this IEndpointRouteBuilder endpoints)
        {
#if NET9_0_OR_GREATER
            // MapOpenApi uses {documentName} as a route parameter to look up the registered
            // OpenAPI document at request time. Call it once with the template pattern.
            endpoints.MapOpenApi("/swagger/{documentName}/swagger.json");
#endif

            return endpoints;
        }

    }

}
