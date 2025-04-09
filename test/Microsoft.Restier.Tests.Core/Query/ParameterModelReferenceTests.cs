// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Tests.Core.Query
{
    using FluentAssertions;
    using Microsoft.OData.Edm;
    using Microsoft.Restier.Core.Query;
    using NSubstitute;
    using Xunit;

    /// <summary>
    /// Unit tests for the <see cref="ParameterModelReference"/> class.
    /// </summary>
    public class ParameterModelReferenceTests
    {
        /// <summary>
        /// Can construct a ParameterModelReference class.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var instance = new ParameterModelReference(Substitute.For<IEdmEntitySet>(), Substitute.For<IEdmType>());
            instance.Should().NotBeNull();
        }
    }
}