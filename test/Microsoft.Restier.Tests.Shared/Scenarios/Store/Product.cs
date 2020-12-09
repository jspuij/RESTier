// <copyright file="Product.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared
{
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Product.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class Product
    {
        /// <summary>
        /// Gets or sets id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets address.
        /// </summary>
        [Required]
        public Address Addr { get; set; }

        /// <summary>
        /// Gets or sets address 2.
        /// </summary>
        public Address Addr2 { get; set; }

        /// <summary>
        /// Gets or sets address 3.
        /// </summary>
        public Address Addr3 { get; set; }
    }
}
