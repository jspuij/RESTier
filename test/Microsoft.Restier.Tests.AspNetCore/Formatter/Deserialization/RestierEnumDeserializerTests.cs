// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using FluentAssertions;
using Microsoft.AspNetCore.OData.Formatter.Deserialization;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder.Annotations;
using Microsoft.Restier.AspNetCore.Formatter;
using NSubstitute;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Formatter
{
    /// <summary>
    /// Unit tests for the <see cref="RestierEnumDeserializer"/> class."/>
    /// </summary>
    public class RestierEnumDeserializerTests
    {
        private readonly RestierEnumDeserializer deserializer;

        public RestierEnumDeserializerTests()
        {
            deserializer = new RestierEnumDeserializer();
        }

        [Fact]
        public void Constructor_ShouldInitialize()
        {
            // Act
            var instance = new RestierEnumDeserializer();

            // Assert
            instance.Should().NotBeNull();
        }

        [Fact]
        public void ReadInline_ShouldReturnEnumValue_WhenResultIsEdmEnumObject()
        {
            // Arrange
            var edmType = Substitute.For<IEdmTypeReference>();
            var enumType = new EdmEnumType("System", "AttributeTargets");
            edmType.Definition.Returns(enumType);
            var readContext = new ODataDeserializerContext();
            readContext.Model = Substitute.For<IEdmModel>();
            
            var edmEnumObject = new ODataEnumValue("Parameter");

            // Act
            var result = deserializer.ReadInline(edmEnumObject, edmType, readContext);

            // Assert
            result.Should().Be(AttributeTargets.Parameter);
        }

        [Fact]
        public void ReadInline_ShouldReturnBaseResult_WhenResultIsNotEdmEnumObject()
        {
            // Arrange
            var edmType = Substitute.For<IEdmTypeReference>();
            edmType.Definition.Returns(new EdmEntityType("System", "Object"));
            var readContext = new ODataDeserializerContext();
            readContext.Model = Substitute.For<IEdmModel>();
            var nonEnumObject = new object();

            // Mock the base method behavior
            var baseDeserializer = Substitute.For<ODataEnumDeserializer>();
            baseDeserializer.ReadInline(nonEnumObject, edmType, readContext).Returns(nonEnumObject);

            // Act
            var result = deserializer.ReadInline(nonEnumObject, edmType, readContext);

            // Assert
            result.Should().Be(nonEnumObject);
        }

        [Fact]
        public void ReadInline_ShouldThrowArgumentNullException_WhenEdmTypeIsNull()
        {
            // Arrange
            var readContext = new ODataDeserializerContext();
            var item = new object();

            // Act
            Action act = () => deserializer.ReadInline(item, null, readContext);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithMessage("*type*");
        }
    }
}
