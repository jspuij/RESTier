// <copyright file="TestableEmptyApi.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.Restier.Core;

    /// <summary>
    /// An API that inherits from <see cref="ApiBase"/> and has no operations or methods.
    /// </summary>
    /// <remarks>
    /// Now that we've separated service registration from API instances, this class can be used many different ways in the tests.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public class TestableEmptyApi : ApiBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestableEmptyApi"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider to use.</param>
        public TestableEmptyApi(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }
    }
}