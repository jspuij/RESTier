// <copyright file="Address.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Address.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class Address
    {
        /// <summary>
        /// Gets or sets the Zip code.
        /// </summary>
        public int Zip { get; set; }
    }
}
