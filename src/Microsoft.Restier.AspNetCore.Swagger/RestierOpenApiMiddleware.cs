// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.OData;
using System;
using System.Threading.Tasks;

namespace Microsoft.Restier.AspNetCore.Swagger
{

    /// <summary>
    /// Middleware that serves OpenAPI documents generated from Restier EDM models.
    /// </summary>
    internal class RestierOpenApiMiddleware
    {

        private readonly RequestDelegate next;
        private readonly IOptions<ODataOptions> odataOptions;
        private readonly Action<OpenApiConvertSettings> openApiSettings;

        public RestierOpenApiMiddleware(
            RequestDelegate next,
            IOptions<ODataOptions> odataOptions,
            Action<OpenApiConvertSettings> openApiSettings = null)
        {
            this.next = next;
            this.odataOptions = odataOptions;
            this.openApiSettings = openApiSettings;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Match requests like /swagger/{documentName}/swagger.json
            var path = context.Request.Path.Value;
            if (path is not null
                && path.StartsWith("/swagger/", StringComparison.OrdinalIgnoreCase)
                && path.EndsWith("/swagger.json", StringComparison.OrdinalIgnoreCase))
            {
                var documentName = path.Substring("/swagger/".Length,
                    path.Length - "/swagger/".Length - "/swagger.json".Length);

                if (!string.IsNullOrEmpty(documentName))
                {
                    var document = RestierOpenApiDocumentGenerator.GenerateDocument(
                        documentName,
                        odataOptions.Value,
                        context.Request,
                        openApiSettings);

                    if (document is not null)
                    {
                        context.Response.ContentType = "application/json; charset=utf-8";
                        var json = document.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
                        await context.Response.WriteAsync(json);
                        return;
                    }
                }
            }

            await next(context);
        }

    }

}
