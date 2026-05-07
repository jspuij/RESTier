// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Tests.Core.Spatial
{
    using FluentAssertions;
    using Microsoft.Restier.Core.Spatial;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Xunit;

    /// <summary>
    /// Unit tests for the <see cref="SpatialAttribute"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class SpatialAttributeTests
    {
        private class Probe
        {
            [Spatial(typeof(string))]
            public object Annotated { get; set; }
        }

        /// <summary>
        /// EdmType returns the constructor argument.
        /// </summary>
        [Fact]
        public void EdmType_returns_constructor_argument()
        {
            var attr = new SpatialAttribute(typeof(int));
            attr.EdmType.Should().Be(typeof(int));
        }

        /// <summary>
        /// Attribute is readable via reflection.
        /// </summary>
        [Fact]
        public void Attribute_is_readable_via_reflection()
        {
            var prop = typeof(Probe).GetProperty(nameof(Probe.Annotated));
            var attr = (SpatialAttribute)Attribute.GetCustomAttribute(prop, typeof(SpatialAttribute));
            attr.Should().NotBeNull();
            attr.EdmType.Should().Be(typeof(string));
        }
    }
}
