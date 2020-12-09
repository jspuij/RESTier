// <copyright file="QueryModelReference.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Query
{
    using System;
    using System.Linq;
    using Microsoft.OData.Edm;

    /// <summary>
    /// Represents a reference to query data in terms of a model.
    /// </summary>
    public class QueryModelReference
    {
        private readonly IEdmEntitySet edmEntitySet;
        private readonly IEdmType edmType;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryModelReference"/> class.
        /// </summary>
        internal QueryModelReference()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryModelReference"/> class.
        /// </summary>
        /// <param name="entitySet">The entity set.</param>
        /// <param name="type">The entity type.</param>
        internal QueryModelReference(IEdmEntitySet entitySet, IEdmType type)
        {
            this.edmEntitySet = entitySet;
            this.edmType = type;
        }

        /// <summary>
        /// Gets the entity set that ultimately contains the data.
        /// </summary>
        public virtual IEdmEntitySet EntitySet => this.edmEntitySet;

        /// <summary>
        /// Gets the type of the data, if any.
        /// </summary>
        public virtual IEdmType Type => this.edmType;
    }
}
