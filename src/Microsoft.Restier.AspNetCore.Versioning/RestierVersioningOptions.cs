// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Asp.Versioning;

namespace Microsoft.Restier.AspNetCore.Versioning
{

    /// <summary>
    /// Per-version options passed to <c>IRestierApiVersioningBuilder.AddVersion</c>.
    /// </summary>
    public sealed class RestierVersioningOptions
    {

        /// <summary>
        /// How to render an <see cref="ApiVersion"/> as the URL segment appended to the base prefix.
        /// Defaults to <see cref="ApiVersionSegmentFormatters.Major"/>.
        /// </summary>
        public Func<ApiVersion, string> SegmentFormatter { get; set; } = ApiVersionSegmentFormatters.Major;

        /// <summary>
        /// Override the composed route prefix entirely. When set, <see cref="SegmentFormatter"/>
        /// and the base prefix are ignored — the supplied value is used verbatim as the
        /// <c>routePrefix</c> argument to <c>AddRestierRoute</c>.
        /// </summary>
        public string ExplicitRoutePrefix { get; set; }

        /// <summary>
        /// Optional sunset date for this version. When set, the headers middleware emits
        /// <c>Sunset: &lt;RFC 1123 date&gt;</c> on responses for routes belonging to this version.
        /// </summary>
        /// <remarks>
        /// <c>[ApiVersion]</c> does not carry sunset metadata, so it must be configured here per call.
        /// Future enhancement: integrate with <c>Asp.Versioning.IPolicyManager</c>.
        /// </remarks>
        public DateTimeOffset? SunsetDate { get; set; }

    }

}
