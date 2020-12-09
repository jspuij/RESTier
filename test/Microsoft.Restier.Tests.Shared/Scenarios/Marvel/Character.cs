// <copyright file="Character.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.Scenarios.Marvel
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Gets or sets the character.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Character
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Character"/> class.
        /// </summary>
        public Character()
        {
            this.ComicsAppearedIn = new ObservableCollection<Comic>();
            this.SeriesStarredIn = new ObservableCollection<Series>();
        }

        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Comics the character appeared in.
        /// </summary>
        public ObservableCollection<Comic> ComicsAppearedIn { get; set; }

        /// <summary>
        /// Gets or sets the Series the character appeared in.
        /// </summary>
        public ObservableCollection<Series> SeriesStarredIn { get; set; }
    }
}
