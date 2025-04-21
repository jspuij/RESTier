// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Restier.AspNetCore;
using NSubstitute;
using System.Net;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Extensions
{
    /// <summary>
    /// Unit tests for the <see cref="RestierHttpRequestExtensions"/> class.
    /// </summary>
    public class RestierHttpRequestExtensionsTests
    {
        [Fact]
        public void IsLocal_ReturnsTrue_WhenRemoteAndLocalIpAreEqual()
        {
            // Arrange
            var httpRequest = CreateHttpRequest(
                remoteIpAddress: IPAddress.Parse("127.0.0.1"),
                localIpAddress: IPAddress.Parse("127.0.0.1")
            );

            // Act
            var result = httpRequest.IsLocal();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsLocal_ReturnsTrue_WhenRemoteIpIsLoopback()
        {
            // Arrange
            var httpRequest = CreateHttpRequest(
                remoteIpAddress: IPAddress.Loopback,
                localIpAddress: null
            );

            // Act
            var result = httpRequest.IsLocal();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsLocal_ReturnsTrue_WhenBothRemoteAndLocalIpAreNull()
        {
            // Arrange
            var httpRequest = CreateHttpRequest(
                remoteIpAddress: null,
                localIpAddress: null
            );

            // Act
            var result = httpRequest.IsLocal();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsLocal_ReturnsFalse_WhenRemoteAndLocalIpAreDifferent()
        {
            // Arrange
            var httpRequest = CreateHttpRequest(
                remoteIpAddress: IPAddress.Parse("192.168.1.1"),
                localIpAddress: IPAddress.Parse("127.0.0.1")
            );

            // Act
            var result = httpRequest.IsLocal();

            // Assert
            Assert.False(result);
        }

        private static HttpRequest CreateHttpRequest(IPAddress remoteIpAddress, IPAddress localIpAddress)
        {
            var httpContext = Substitute.For<HttpContext>();
            var connectionFeature = Substitute.For<ConnectionInfo>();
            connectionFeature.RemoteIpAddress.Returns(remoteIpAddress);
            connectionFeature.LocalIpAddress.Returns(localIpAddress);
            httpContext.Connection.Returns(connectionFeature);

            var httpRequest = Substitute.For<HttpRequest>();
            httpRequest.HttpContext.Returns(httpContext);

            return httpRequest;
        }
    }
}
