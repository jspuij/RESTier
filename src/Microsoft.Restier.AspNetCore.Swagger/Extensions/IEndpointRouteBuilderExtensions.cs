// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Swagger;

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
            var odataOptions = endpoints.ServiceProvider
                .GetRequiredService<IOptions<ODataOptions>>().Value;

            foreach (var prefix in odataOptions.GetRestierRoutePrefixes())
            {
                var documentName = string.IsNullOrEmpty(prefix)
                    ? RestierOpenApiDocumentGenerator.DefaultDocumentName
                    : prefix;

                endpoints.MapOpenApi($"/swagger/{documentName}/swagger.json");
            }
#endif

            return endpoints;
        }

    }

}
