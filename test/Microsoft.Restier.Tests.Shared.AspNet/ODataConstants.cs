// <copyright file="ODataConstants.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.AspNet
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// A set of constants used by Breakdance.OData to simplify the configuration of test runs.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ODataConstants
    {
        /// <summary>
        /// Specifies the Accept HTTP header required for OData calls.
        /// </summary>
        public const string DefaultAcceptHeader = "application/json;odata.metadata=full";

        /// <summary>
        /// Specifies the Accept HTTP header required for OData calls.
        /// </summary>
        public const string MinimalAcceptHeader = "application/json;odata.metadata=minimal";
    }
}