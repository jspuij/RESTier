// <copyright file="MarvelContext.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.EntityFramework.Scenarios.Marvel
{
    using System;
    using System.Collections.ObjectModel;
    using System.Data.Entity;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.Restier.Tests.Shared.Scenarios.Marvel;

    /// <summary>
    /// DbContext for the Marvel databse.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class MarvelContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MarvelContext"/> class.
        /// </summary>
        public MarvelContext()
            : base("MarvelContext") => Database.SetInitializer(new MarvelTestInitializer());

        /// <summary>
        /// Gets or sets the set of characters.
        /// </summary>
        public IDbSet<Character> Characters { get; set; }

        /// <summary>
        /// Gets or sets the set of comics.
        /// </summary>
        public IDbSet<Comic> Comics { get; set; }

        /// <summary>
        /// Gets or sets the set of Series.
        /// </summary>
        public IDbSet<Series> Series { get; set; }
    }
}