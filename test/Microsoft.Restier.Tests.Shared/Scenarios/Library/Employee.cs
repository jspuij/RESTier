// <copyright file="Employee.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Employee.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Employee
    {
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the full name.
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Gets or sets the Address.
        /// </summary>
        public Address Addr { get; set; }

        /// <summary>
        /// Gets or sets the Universe.
        /// </summary>
        public Universe Universe { get; set; }
    }
}