// <copyright file="ChangeSetItem.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Submit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Net;

    /// <summary>
    /// Specifies the type of a change set item.
    /// </summary>
    internal enum ChangeSetItemType
    {
        /// <summary>
        /// Specifies a data modification item.
        /// </summary>
        DataModification,
    }

    /// <summary>
    /// Possible states of an resource during a ChangeSet life cycle.
    /// </summary>
    internal enum ChangeSetItemProcessingStage
    {
        /// <summary>
        /// If an new change set item is created.
        /// </summary>
        Initialized,

        /// <summary>
        /// The resource has been validated.
        /// </summary>
        Validated,

        /// <summary>
        /// The resource set deleting, inserting or updating events are raised.
        /// </summary>
        PreEventing,

        /// <summary>
        /// The resource was modified within its own pre eventing interception method. This indicates that the resource
        /// should be revalidated but its pre eventing interception point should not be invoked again.
        /// </summary>
        ChangedWithinOwnPreEventing,

        /// <summary>
        /// The resource's pre events have been raised.
        /// </summary>
        PreEvented,
    }

    /// <summary>
    /// Represents an item in a change set.
    /// </summary>
    public abstract class ChangeSetItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeSetItem"/> class.
        /// </summary>
        /// <param name="type">The change set item type.</param>
        internal ChangeSetItem(ChangeSetItemType type)
        {
            this.Type = type;
            this.ChangeSetItemProcessingStage = ChangeSetItemProcessingStage.Initialized;
        }

        /// <summary>
        /// Gets the type of this change set item.
        /// </summary>
        internal ChangeSetItemType Type { get; private set; }

        /// <summary>
        /// Gets or sets the dynamic state of this change set item.
        /// </summary>
        internal ChangeSetItemProcessingStage ChangeSetItemProcessingStage { get; set; }

        /// <summary>
        /// Indicates whether this change set item is in a changed state.
        /// </summary>
        /// <returns>
        /// Whether this change set item is in a changed state.
        /// </returns>
        public bool HasChanged()
        {
            return this.ChangeSetItemProcessingStage == ChangeSetItemProcessingStage.Initialized ||
                this.ChangeSetItemProcessingStage == ChangeSetItemProcessingStage.ChangedWithinOwnPreEventing;
        }
    }
}
