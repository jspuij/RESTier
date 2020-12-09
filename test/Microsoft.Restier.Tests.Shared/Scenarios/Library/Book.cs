// <copyright file="Book.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Book.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Book
    {
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the ISBN.
        /// </summary>
        [MinLength(13)]
        [MaxLength(13)]
        public string Isbn { get; set; }

        /// <summary>
        /// Gets or sets the Title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the Publisher.
        /// </summary>
        public Publisher Publisher { get; set; }
    }
}