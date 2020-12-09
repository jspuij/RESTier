// <copyright file="LibraryContext.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.EntityFramework.Scenarios.Library
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data.Entity;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.OData.Edm;
    using Microsoft.Restier.Tests.Shared.Scenarios.Library;

    /// <summary>
    /// A Sample Database context for Libraries.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class LibraryContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryContext"/> class.
        /// </summary>
        public LibraryContext()
            : base("LibraryContext") => Database.SetInitializer(new LibraryTestInitializer());

        /// <summary>
        /// Gets or sets a set of books.
        /// </summary>
        public IDbSet<Book> Books { get; set; }

        /// <summary>
        /// Gets or sets a set of library cards.
        /// </summary>
        public IDbSet<LibraryCard> LibraryCards { get; set; }

        /// <summary>
        /// Gets or sets a set of publishers.
        /// </summary>
        public IDbSet<Publisher> Publishers { get; set; }

        /// <summary>
        /// Gets or sets a set of readers.
        /// </summary>
        public IDbSet<Employee> Readers { get; set; }
    }
}
