// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if NET9_0_OR_GREATER

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.OData;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.AspNetCore.Swagger
{

    /// <summary>
    /// An <see cref="IOpenApiDocumentTransformer"/> that replaces the auto-generated OpenAPI document
    /// with one generated from the Restier EDM model.
    /// </summary>
    public class RestierOpenApiDocumentTransformer : IOpenApiDocumentTransformer
    {

        private readonly Action<OpenApiConvertSettings> openApiSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierOpenApiDocumentTransformer"/> class.
        /// </summary>
        /// <param name="openApiSettings">An optional action to configure <see cref="OpenApiConvertSettings"/>.</param>
        public RestierOpenApiDocumentTransformer(Action<OpenApiConvertSettings> openApiSettings = null)
        {
            this.openApiSettings = openApiSettings;
        }

        /// <summary>
        /// Transforms the OpenAPI document by replacing it with one generated from the EDM model.
        /// </summary>
        /// <param name="document">The document to transform.</param>
        /// <param name="context">The transformer context.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A completed task.</returns>
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            var services = context.ApplicationServices;
            var odataOptions = services.GetRequiredService<IOptions<ODataOptions>>().Value;
            var httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();

            var generated = RestierOpenApiDocumentGenerator.GenerateDocument(
                context.DocumentName,
                odataOptions,
                httpContextAccessor.HttpContext?.Request,
                openApiSettings);

            if (generated is not null)
            {
                // Replace the auto-generated document content with the EDM-based document.
                document.Info = generated.Info;
                document.Servers = generated.Servers;
                document.Paths = generated.Paths;
                document.Components = generated.Components;
                document.Tags = generated.Tags;
                document.ExternalDocs = generated.ExternalDocs;
                document.Extensions = generated.Extensions;
            }

            return Task.CompletedTask;
        }

    }

}

#endif
