// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Options;

namespace Microsoft.Restier.AspNetCore.Versioning
{
    // Placeholder; full composite implementation lands in Task 12.
    internal sealed class RestierApiVersionDescriptionProvider : IApiVersionDescriptionProvider
    {
        public RestierApiVersionDescriptionProvider(
            IOptions<ODataOptions> odataOptions,
            IRestierApiVersionRegistry registry,
            IApiVersionDescriptionProvider inner)
        {
        }

        public IReadOnlyList<ApiVersionDescription> ApiVersionDescriptions => throw new NotImplementedException();

        public bool IsDeprecated(ApiVersion apiVersion) => throw new NotImplementedException();
    }
}
