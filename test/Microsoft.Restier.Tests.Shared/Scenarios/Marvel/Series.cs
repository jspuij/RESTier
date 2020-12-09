// <copyright file="Series.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.Scenarios.Marvel
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Gets or sets the series.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Series
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Series"/> class.
        /// </summary>
        public Series()
        {
            this.Comics = new ObservableCollection<Comic>();
            this.MainCharacters = new ObservableCollection<Character>();
        }

        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the main characters.
        /// </summary>
        public ObservableCollection<Character> MainCharacters { get; set; }

        /// <summary>
        /// Gets or sets the comics.
        /// </summary>
        public ObservableCollection<Comic> Comics { get; set; }
    }
}
