// <copyright file="MarvelApi.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.AspNet.Scenarios.Marvel
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Microsoft.Restier.AspNet.Model;
    using Microsoft.Restier.EntityFramework;
    using Microsoft.Restier.Tests.Shared.EntityFramework.Scenarios.Marvel;

    /// <summary>
    /// A testable API that implements an Entity Framework model and has secondary operations
    /// against a SQL 2017 LocalDB database.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class MarvelApi : EntityFrameworkApi<MarvelContext>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MarvelApi"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public MarvelApi(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }
    }
}