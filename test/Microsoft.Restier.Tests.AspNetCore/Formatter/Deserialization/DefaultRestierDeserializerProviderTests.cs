// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using FluentAssertions;
using Microsoft.AspNetCore.OData.Formatter.Deserialization;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Formatter;
using NSubstitute;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Formatter
{
    /// <summary>
    /// unit tests for the <see cref="DefaultRestierDeserializerProvider"/>
    /// </summary>
    public class DefaultRestierDeserializerProviderTests
    {
        [Fact]
        public void Constructor_ShouldInitializeEnumDeserializer()
        {
            // Arrange
            var serviceProvider = Substitute.For<IServiceProvider>();

            // Act
            var provider = new DefaultRestierDeserializerProvider(serviceProvider);

            // Assert
            provider.Should().NotBeNull();
        }

        [Fact]
        public void GetEdmTypeDeserializer_ShouldReturnEnumDeserializer_WhenEdmTypeIsEnum()
        {
            // Arrange
            var serviceProvider = Substitute.For<IServiceProvider>();
            var provider = new DefaultRestierDeserializerProvider(serviceProvider);
            var edmType = Substitute.For<IEdmTypeReference>();
            edmType.Definition.Returns(new EdmEnumType("Test", "Test"));

            // Act
            var deserializer = provider.GetEdmTypeDeserializer(edmType);

            // Assert
            deserializer.Should().BeOfType<RestierEnumDeserializer>();
        }

        [Fact]
        public void GetEdmTypeDeserializer_ShouldCallBaseMethod_WhenEdmTypeIsNotEnum()
        {
            // Arrange
            var serviceProvider = Substitute.For<IServiceProvider>();
            var provider = new DefaultRestierDeserializerProvider(serviceProvider);
            serviceProvider.GetService(typeof(ODataResourceDeserializer))
                .Returns(Substitute.For<ODataResourceDeserializer>(provider));
            var edmType = Substitute.For<IEdmTypeReference>();
            edmType.Definition.Returns(new EdmEntityType("Test","Test"));


            // Act
            var deserializer = provider.GetEdmTypeDeserializer(edmType);

            // Assert
            deserializer.Should().NotBeOfType<RestierEnumDeserializer>();
        }
    }
}
