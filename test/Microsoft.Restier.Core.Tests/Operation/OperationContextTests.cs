// <copyright file="OperationContextTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Tests.Operation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using FluentAssertions;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Operation;
    using Microsoft.Restier.Tests.Shared;
    using Moq;
    using Xunit;

    /// <summary>
    /// Unit tests for the <see cref="OperationContext"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class OperationContextTests : IClassFixture<ServiceProviderFixture>
    {
        private OperationContext testClass;
        private ApiBase api;
        private Func<string, object> getParameterValueFunc;
        private string operationName;
        private bool isFunction;
        private IEnumerable bindingParameterValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationContextTests"/> class.
        /// </summary>
        /// <param name="serviceProviderFixture">Fixture for <see cref="IServiceProvider"/> instance.</param>
        public OperationContextTests(ServiceProviderFixture serviceProviderFixture)
        {
            this.api = new TestApi(serviceProviderFixture.ServiceProvider);
            this.getParameterValueFunc = name => this;
            this.operationName = "Insert";
            this.isFunction = true;
            this.bindingParameterValue = new List<object>();
            this.testClass = new OperationContext(
                this.api,
                this.getParameterValueFunc,
                this.operationName,
                this.isFunction,
                this.bindingParameterValue);
        }

        /// <summary>
        /// Can construct a new <see cref="OperationContext"/>.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var instance = new OperationContext(
                this.api,
                this.getParameterValueFunc,
                this.operationName,
                this.isFunction,
                this.bindingParameterValue);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot construct the <see cref="OperationContext"/> with a null Api.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullApi()
        {
            Action act = () => new OperationContext(
                default(ApiBase),
                default(Func<string, object>),
                "TestValue719188563",
                true,
                new Mock<IEnumerable>().Object);
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot construct the <see cref="OperationContext"/> with a null getParameterValueFunc.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullGetParameterValueFunc()
        {
            Action act = () => new OperationContext(
                this.api,
                default(Func<string, object>),
                "TestValue734278354",
                false,
                new Mock<IEnumerable>().Object);
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot construct the <see cref="OperationContext"/> with a null bindingParameterValue.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullBindingParameterValue()
        {
            Action act = () => new OperationContext(
                this.api,
                default(Func<string, object>),
                "TestValue715530316",
                true,
                default(IEnumerable));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot construct the <see cref="OperationContext"/> with an invalid OperationName.
        /// </summary>
        /// <param name="value">OperationName.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CannotConstructWithInvalidOperationName(string value)
        {
            Action act = () => new OperationContext(
                this.api,
                default(Func<string, object>),
                value,
                false,
                new Mock<IEnumerable>().Object);
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Test that the Operation name is initialized correctly.
        /// </summary>
        [Fact]
        public void OperationNameIsInitializedCorrectly()
        {
            this.testClass.OperationName.Should().Be(this.operationName);
        }

        /// <summary>
        /// Tests that the getParameterValueFunc is initialized correctly.
        /// </summary>
        [Fact]
        public void GetParameterValueFuncIsInitializedCorrectly()
        {
            this.testClass.GetParameterValueFunc.Should().Be(this.getParameterValueFunc);
        }

        /// <summary>
        /// Tests that the isFunction property is initialized correctly.
        /// </summary>
        [Fact]
        public void IsFunctionIsInitializedCorrectly()
        {
            this.testClass.IsFunction.Should().Be(this.isFunction);
        }

        /// <summary>
        /// Tests that the bindingParameterValue is initialized correctly.
        /// </summary>
        [Fact]
        public void BindingParameterValueIsInitializedCorrectly()
        {
            this.testClass.BindingParameterValue.Should().BeEquivalentTo(this.bindingParameterValue);
        }

        /// <summary>
        /// Tests that ParameterValues can be set and get.
        /// </summary>
        [Fact]
        public void CanSetAndGetParameterValues()
        {
            var testValue = new List<object>();
            this.testClass.ParameterValues = testValue;
            this.testClass.ParameterValues.Should().BeEquivalentTo(testValue);
        }

        private class TestApi : ApiBase
        {
            public TestApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }
        }
    }
}