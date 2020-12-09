// <copyright file="QueryModelReferenceTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Tests.Query
{
    using System.Diagnostics.CodeAnalysis;
    using FluentAssertions;
    using Microsoft.OData.Edm;
    using Microsoft.Restier.Core.Query;
    using Moq;
    using Xunit;

    /// <summary>
    /// Unit tests for the <see cref="QueryModelReference" /> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class QueryModelReferenceTests
    {
        /// <summary>
        /// Can get the entity set.
        /// </summary>
        [Fact]
        public void CanGetEntitySet()
        {
            var edmEntitySet = new Mock<IEdmEntitySet>().Object;
            var edmType = new Mock<IEdmType>().Object;
            var instance = new QueryModelReference(edmEntitySet, edmType);
            instance.EntitySet.Should().Be(edmEntitySet);
        }

        /// <summary>
        /// Can get the type.
        /// </summary>
        [Fact]
        public void CanGetType()
        {
            var edmEntitySet = new Mock<IEdmEntitySet>().Object;
            var edmType = new Mock<IEdmType>().Object;
            var instance = new QueryModelReference(edmEntitySet, edmType);
            instance.Type.Should().Be(edmType);
        }
    }
}