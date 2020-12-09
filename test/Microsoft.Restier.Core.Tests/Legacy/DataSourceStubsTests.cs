// <copyright file="DataSourceStubsTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Core
{
    using System;
    using FluentAssertions;
    using Microsoft.Restier.Core;
    using Xunit;

    /// <summary>
    /// Tests that you cannot call anything on the DataSourceStub.
    /// </summary>
    public class DataSourceStubsTests
    {
        /// <summary>
        /// Tests that GetQueryableSource is not callable.
        /// </summary>
        [Fact]
        public void SourceOfEntityContainerElementIsNotCallable()
        {
            Action invalidOperation = () => { DataSourceStub.GetQueryableSource<object>("EntitySet"); };
            invalidOperation.Should().Throw<InvalidOperationException>();
        }

        /// <summary>
        /// Tests that GetQueryableSource for a composable function is not callable.
        /// </summary>
        [Fact]
        public void SourceOfComposableFunctionIsNotCallable()
        {
            Action invalidOperation = () => { DataSourceStub.GetQueryableSource<object>("Namespace", "Function"); };
            invalidOperation.Should().Throw<InvalidOperationException>();
        }

        // TODO enable these when function/action is supported.
        // [TestMethod]
        // public void ResultsOfEntityContainerElementIsNotCallable()
        // {
        //     Assert.Throws<InvalidOperationException>(() => DataSourceStub.Results<object>("EntitySet"));
        // }

        // [TestMethod]
        // public void ResultOfEntityContainerElementIsNotCallable()
        // {
        //     Assert.Throws<InvalidOperationException>(() => DataSourceStub.Result<object>("Singleton"));
        // }

        // [TestMethod]
        // public void ResultsOfComposableFunctionIsNotCallable()
        // {
        //     Assert.Throws<InvalidOperationException>(() => DataSourceStub.Results<object>("Namespace", "Function"));
        // }

        // [TestMethod]
        // public void ResultOfComposableFunctionIsNotCallable()
        // {
        //     Assert.Throws<InvalidOperationException>(() => DataSourceStub.Result<object>("Namespace", "Function"));
        // }

        /// <summary>
        /// GetPropertyValue is not callable.
        /// </summary>
        [Fact]
        public void ValueIsNotCallable()
        {
            Action invalidOperation = () => { DataSourceStub.GetPropertyValue<object>(new object(), "Property"); };
            invalidOperation.Should().Throw<InvalidOperationException>();
        }
    }
}
