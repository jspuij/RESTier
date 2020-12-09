// <copyright file="DisallowEverythingAuthorizer.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared
{
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.Restier.Core.Query;

    /// <summary>
    /// An <see cref="IQueryExpressionAuthorizer"/> implementation that always returns false.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class DisallowEverythingAuthorizer : IQueryExpressionAuthorizer
    {
        /// <summary>
        /// This method will always return false.
        /// </summary>
        /// <param name="context">The Query context passed.</param>
        /// <returns>false every time.</returns>
        public bool Authorize(QueryExpressionContext context) => false;
    }
}