// <copyright file="Publisher.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// The publisher.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Publisher
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Publisher"/> class.
        /// </summary>
        public Publisher()
        {
            this.Books = new ObservableCollection<Book>();
        }

        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the address.
        /// </summary>
        public Address Addr { get; set; }

        /// <summary>
        /// Gets or sets the Books.
        /// </summary>
        public virtual ObservableCollection<Book> Books { get; set; }
    }
}
