// <copyright file="StoreApi.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.Restier.Core;

    /// <summary>
    /// Store Api.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class StoreApi : ApiBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StoreApi"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider to use.</param>
        public StoreApi(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }
    }
}