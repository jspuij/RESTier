// <copyright file="Comic.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.Scenarios.Marvel
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// The comic.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Comic
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Comic"/> class.
        /// </summary>
        public Comic()
        {
            this.Characters = new ObservableCollection<Character>();
        }

        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the ISBN Number.
        /// </summary>
        [MinLength(13)]
        [MaxLength(13)]
        public string Isbn { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the issue number.
        /// </summary>
        public int IssueNumber { get; set; }

        /// <summary>
        /// Gets or sets the characters.
        /// </summary>
        public virtual ObservableCollection<Character> Characters { get; set; }

        /// <summary>
        /// Gets or sets the series.
        /// </summary>
        public Series Series { get; set; }
    }
}
