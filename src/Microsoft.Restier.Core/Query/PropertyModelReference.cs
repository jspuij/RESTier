// <copyright file="PropertyModelReference.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Query
{
    using System;
    using Microsoft.OData.Edm;

    /// <summary>
    /// Represents a reference to property data in terms of a model.
    /// </summary>
    public class PropertyModelReference : QueryModelReference
    {
        private readonly string propertyName;
        private IEdmProperty property;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyModelReference" /> class.
        /// </summary>
        /// <param name="source">
        /// A source query model reference.
        /// </param>
        /// <param name="propertyName">
        /// The name of a property.
        /// </param>
        /// <param name="property">
        /// EDM property instance.
        /// </param>
        internal PropertyModelReference(QueryModelReference source, string propertyName, IEdmProperty property)
        {
            Ensure.NotNull(propertyName, nameof(propertyName));
            this.propertyName = propertyName;

            Ensure.NotNull(property, nameof(property));
            this.property = property;
            this.Source = source;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyModelReference" /> class.
        /// </summary>
        /// <param name="source">
        /// A source query model reference.
        /// </param>
        /// <param name="propertyName">
        /// The name of a property.
        /// </param>
        internal PropertyModelReference(QueryModelReference source, string propertyName)
        {
            Ensure.NotNull(propertyName, nameof(propertyName));
            this.propertyName = propertyName;

            Ensure.NotNull(source, nameof(source));
            this.Source = source;
        }

        /// <summary>
        /// Gets the source of the derived data.
        /// </summary>
        public QueryModelReference Source { get; private set; }

        /// <summary>
        /// Gets the entity set that contains the data.
        /// </summary>
        public override IEdmEntitySet EntitySet
        {
            get
            {
                if (this.Source != null)
                {
                    return this.Source.EntitySet;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the type of the queryable data.
        /// </summary>
        public override IEdmType Type
        {
            get
            {
                if (this.Property != null)
                {
                    return this.Property.Type.Definition;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the property representing the property data.
        /// </summary>
        public IEdmProperty Property
        {
            get
            {
                if (this.property != null)
                {
                    return this.property;
                }

                if (this.Source != null)
                {
                    var structuredType = this.Source.Type as IEdmStructuredType;
                    if (structuredType != null)
                    {
                        this.property = structuredType.FindProperty(this.propertyName);
                        return this.property;
                    }
                }

                return null;
            }
        }
    }
}
