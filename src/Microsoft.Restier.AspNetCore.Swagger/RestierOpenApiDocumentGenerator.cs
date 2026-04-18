// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Microsoft.OpenApi.OData;
using Microsoft.Restier.AspNetCore;
using System;
using System.Linq;

namespace Microsoft.Restier.AspNetCore.Swagger
{

    /// <summary>
    /// Generates OpenAPI documents from Restier EDM models.
    /// Shared logic used by both the net8.0 middleware and the net9.0+ document transformer.
    /// </summary>
    internal static class RestierOpenApiDocumentGenerator
    {

        /// <summary>
        /// The document name used for Restier routes registered with an empty prefix.
        /// </summary>
        public const string DefaultDocumentName = "default";

        /// <summary>
        /// Generates an <see cref="OpenApiDocument"/> for the specified Restier route.
        /// </summary>
        /// <param name="documentName">The document name.</param>
        /// <param name="odataOptions">The OData options.</param>
        /// <param name="request">The current HTTP request, or null.</param>
        /// <param name="openApiSettings">Optional settings configurator.</param>
        /// <returns>The generated document, or null if the route was not found.</returns>
        public static OpenApiDocument GenerateDocument(
            string documentName,
            ODataOptions odataOptions,
            HttpRequest request,
            Action<OpenApiConvertSettings> openApiSettings)
        {
            var routePrefix = string.Equals(documentName, DefaultDocumentName, StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : documentName;

            if (!odataOptions.RouteComponents.TryGetValue(routePrefix, out var routeComponent))
            {
                return null;
            }

            var model = routeComponent.EdmModel;
            var routeServices = odataOptions.GetRouteServices(routePrefix);
            var odataValidationSettings = routeServices.GetService<ODataValidationSettings>();

            // @robertmclaws: Start off by setting defaults, but allow the user to override it.
            var settings = new OpenApiConvertSettings { TopExample = odataValidationSettings?.MaxTop ?? 5 };
            openApiSettings?.Invoke(settings);

            // @robertmclaws: The host defaults internally to localhost; isn't set automatically.
            if (request is not null)
            {
                var pathParts = new[]
                {
                    // @robertmclaws: You're going to think the next line is an error and want to put the second slash in.
                    //                Don't. The second slash will be added with the string.Join(). ;)
                    $"{request.Scheme}:/",
                    request.Host.Value,
                    request.PathBase.HasValue ? request.PathBase.Value.TrimStart('/') : null,
                    routePrefix
                };
                settings.ServiceRoot = new Uri(string.Join("/", pathParts.Where(c => !string.IsNullOrWhiteSpace(c))));
            }

            return model.ConvertToOpenApi(settings);
        }

    }

}
