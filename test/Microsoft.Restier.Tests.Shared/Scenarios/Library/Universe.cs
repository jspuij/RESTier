// <copyright file="Universe.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.OData.Edm;

    /// <summary>
    /// The universe.
    /// </summary>
    [ComplexType]
    [ExcludeFromCodeCoverage]
    public class Universe
    {
        /// <summary>
        /// Gets or sets the Binary property.
        /// </summary>
        public byte[] BinaryProperty { get; set; }

        /// <summary>
        /// Gets or sets the Boolean property.
        /// </summary>
#pragma warning disable SA1623 // Property summary documentation should match accessors
        public bool BooleanProperty { get; set; }
#pragma warning restore SA1623 // Property summary documentation should match accessors

        /// <summary>
        /// Gets or sets the Byte property.
        /// </summary>
        public byte ByteProperty { get; set; }

        // public Date DateProperty { get; set; }

        /// <summary>
        /// Gets or sets the DateTimeOffset property.
        /// </summary>
        public DateTimeOffset DateTimeOffsetProperty { get; set; }

        /// <summary>
        /// Gets or sets the Decimal property.
        /// </summary>
        public decimal DecimalProperty { get; set; }

        /// <summary>
        /// Gets or sets the Double property.
        /// </summary>
        public double DoubleProperty { get; set; }

        /// <summary>
        /// Gets or sets the Duration property.
        /// </summary>
        public TimeSpan DurationProperty { get; set; }

        /// <summary>
        /// Gets or sets the Guid property.
        /// </summary>
        public Guid GuidProperty { get; set; }

        /// <summary>
        /// Gets or sets the short property.
        /// </summary>
        public short Int16Property { get; set; }

        /// <summary>
        /// Gets or sets the int property.
        /// </summary>
        public int Int32Property { get; set; }

        /// <summary>
        /// Gets or sets the long property.
        /// </summary>
        public long Int64Property { get; set; }

        // public sbyte SByteProperty { get; set; }

        /// <summary>
        /// Gets or sets the Single property.
        /// </summary>
        public float SingleProperty { get; set; }

        // public FileStream StreamProperty { get; set; }

        /// <summary>
        /// Gets or sets the string property.
        /// </summary>
        public string StringProperty { get; set; }

        /// <summary>
        /// Gets or sets the Time of day property.
        /// </summary>
        public TimeOfDay TimeOfDayProperty { get; set; }
    }
}
