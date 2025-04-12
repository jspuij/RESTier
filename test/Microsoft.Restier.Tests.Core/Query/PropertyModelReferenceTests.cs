// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;
using NSubstitute;
using System;
using Xunit;

namespace Microsoft.Restier.Tests.Core.Query
{
    /// <summary>
    /// Unit tests for the <see cref="PropertyModelReference"/> tests.
    /// </summary>
    public class PropertyModelReferenceTests
    {
        /// <summary>
        /// Can construct an instance of <see cref="PropertyModelReference"/>.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var instance = new PropertyModelReference(new QueryModelReference(), "Name");
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Can construct an instance of <see cref="PropertyModelReference"/> with three arguments.
        /// </summary>
        [Fact]
        public void CanConstructThreeArgs()
        {
            var edmProperty = Substitute.For<IEdmProperty>();
            var instance = new PropertyModelReference(new QueryModelReference(), "Name", edmProperty);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Can get the source.
        /// </summary>
        [Fact]
        public void CanGetSource()
        {
            var queryModelReference = new QueryModelReference();
            var edmProperty = Substitute.For<IEdmProperty>();
            var instance = new PropertyModelReference(queryModelReference, "Name", edmProperty);
            instance.Source.Should().Be(queryModelReference);
        }

        /// <summary>
        /// Can get the EntitySet.
        /// </summary>
        [Fact]
        public void CanGetEntitySet()
        {
            var edmEntitySet = Substitute.For<IEdmEntitySet>();
            var edmType = Substitute.For<IEdmType>();
            var queryModelReference = new QueryModelReference(edmEntitySet, edmType);
            var edmProperty = Substitute.For<IEdmProperty>();
            var instance = new PropertyModelReference(queryModelReference, "Name", edmProperty);
            instance.EntitySet.Should().Be(edmEntitySet);
        }

        /// <summary>
        /// Cannot get the entitySet.
        /// </summary>
        [Fact]
        public void CannotHaveDefaultQueryReference()
        {
            var edmProperty = Substitute.For<IEdmProperty>();
            var act = () => new PropertyModelReference(default(QueryModelReference), "Name", edmProperty);
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Can get the type.
        /// </summary>
        [Fact]
        public void CanGetType()
        {
            var edmEntitySet = Substitute.For<IEdmEntitySet>();
            var edmType = Substitute.For<IEdmType>();
            var queryModelReference = new QueryModelReference(edmEntitySet, edmType);
            var propertyTypeReference = Substitute.For<IEdmTypeReference>();
            var edmProperty = Substitute.For<IEdmProperty>();
            edmProperty.Type.Returns(propertyTypeReference);
            var propertyType = Substitute.For<IEdmType>();
            propertyTypeReference.Definition.Returns(propertyType);
            var instance = new PropertyModelReference(queryModelReference, "Name", edmProperty);
            instance.Type.Should().Be(propertyType);
        }

        /// <summary>
        /// Cannot get the type.
        /// </summary>
        [Fact]
        public void CannotGetType()
        {
            var edmEntitySet = Substitute.For<IEdmEntitySet>();
            var edmType = Substitute.For<IEdmType>();
            var queryModelReference = new QueryModelReference(edmEntitySet, edmType);
            var instance = new PropertyModelReference(queryModelReference, "Name");
            instance.Type.Should().BeNull();
        }

        /// <summary>
        /// Can get a property.
        /// </summary>
        [Fact]
        public void CanGetProperty()
        {
            var edmEntitySet = Substitute.For<IEdmEntitySet>();
            var edmType = Substitute.For<IEdmType>();
            var queryModelReference = new QueryModelReference(edmEntitySet, edmType);
            var edmProperty = Substitute.For<IEdmProperty>();
            var instance = new PropertyModelReference(queryModelReference, "Name", edmProperty);
            instance.Property.Should().Be(edmProperty);
        }

        /// <summary>
        /// Can get a property.
        /// </summary>
        [Fact]
        public void CanGetPropertyThroughReference()
        {
            var edmEntitySet = Substitute.For<IEdmEntitySet>();
            var edmType = Substitute.For<IEdmType, IEdmStructuredType>();
            var edmStructuredType = edmType as IEdmStructuredType;
            var queryModelReference = new QueryModelReference(edmEntitySet, edmType);
            var edmProperty = Substitute.For<IEdmProperty>();
            edmStructuredType?.FindProperty(Arg.Any<string>()).Returns(edmProperty);
            var instance = new PropertyModelReference(queryModelReference, "Name");
            instance.Property.Should().Be(edmProperty);
        }

        /// <summary>
        /// Can get a property.
        /// </summary>
        [Fact]
        public void CannotGetProperty()
        {
            var edmEntitySet = Substitute.For<IEdmEntitySet>();
            var edmType = Substitute.For<IEdmType>();
            var queryModelReference = new QueryModelReference(edmEntitySet, edmType);
            var instance = new PropertyModelReference(queryModelReference, "Name");
            instance.Property.Should().BeNull();
        }
    }
}