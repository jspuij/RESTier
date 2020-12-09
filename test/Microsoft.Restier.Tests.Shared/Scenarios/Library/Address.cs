// <copyright file="Address.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Address.
    /// </summary>
    [ComplexType]
    [ExcludeFromCodeCoverage]
    public class Address
    {
        /// <summary>
        /// Gets or sets the Street.
        /// </summary>
        public string Street { get; set; }

        /// <summary>
        /// Gets or sets the Zip Code.
        /// </summary>
        public string Zip { get; set; }
    }
}
