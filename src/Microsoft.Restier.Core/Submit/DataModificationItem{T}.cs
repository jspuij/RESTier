// <copyright file="DataModificationItem{T}.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Submit
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a data modification item in a change set.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    public class DataModificationItem<T> : DataModificationItem
        where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataModificationItem{T}" /> class.
        /// </summary>
        /// <param name="resourceSetName">
        /// The name of the resource set in question.
        /// </param>
        /// <param name="expectedResourceType">
        /// The type of the expected resource type in question.
        /// </param>
        /// <param name="actualResourceType">
        /// The type of the actual resource type in question.
        /// </param>
        /// <param name="action">
        /// The RestierEntitySetOperations for the request.
        /// </param>
        /// <param name="resourceKey">
        /// The key of the resource being modified.
        /// </param>
        /// <param name="originalValues">
        /// Any original values of the resource that are known.
        /// </param>
        /// <param name="localValues">
        /// The local values of the entity.
        /// </param>
        public DataModificationItem(
            string resourceSetName,
            Type expectedResourceType,
            Type actualResourceType,
            RestierEntitySetOperation action,
            IReadOnlyDictionary<string, object> resourceKey,
            IReadOnlyDictionary<string, object> originalValues,
            IReadOnlyDictionary<string, object> localValues)
            : base(
                  resourceSetName,
                  expectedResourceType,
                  actualResourceType,
                  action,
                  resourceKey,
                  originalValues,
                  localValues)
        {
        }

        /// <summary>
        /// Gets or sets the resource object in question.
        /// </summary>
        /// <remarks>
        /// Initially this will be <c>null</c>, however after the change
        /// set has been prepared it will represent the pending resource.
        /// </remarks>
        public new T Resource
        {
            get => base.Resource as T;

            set => base.Resource = value;
        }
    }
}
