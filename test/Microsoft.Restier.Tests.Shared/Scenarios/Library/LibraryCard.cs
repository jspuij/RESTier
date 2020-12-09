// <copyright file="LibraryCard.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// An object in the model that is supposed to remain empty for unit tests.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class LibraryCard
    {
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the Date registered.
        /// </summary>
        public DateTimeOffset DateRegistered { get; set; }
    }
}