// <copyright file="ChangeSetItemValidationResultTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Tests.Submit
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Tracing;
    using FluentAssertions;
    using Microsoft.Restier.Core.Submit;
    using Xunit;

    /// <summary>
    /// Unit tests for the <see cref="ChangeSetItemValidationResult"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ChangeSetItemValidationResultTests
    {
        private ChangeSetItemValidationResult testClass;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeSetItemValidationResultTests"/> class.
        /// </summary>
        public ChangeSetItemValidationResultTests()
        {
            this.testClass = new ChangeSetItemValidationResult();
        }

        /// <summary>
        /// Can construct an instance.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var instance = new ChangeSetItemValidationResult();
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Can call the ToString() method.
        /// </summary>
        [Fact]
        public void CanCallToString()
        {
            this.testClass.Message = "Lorem ipsum";
            var result = this.testClass.ToString();
            result.Should().Be(this.testClass.Message);
        }

        /// <summary>
        /// Can get and set the Validator type.
        /// </summary>
        [Fact]
        public void CanSetAndGetValidatorType()
        {
            var testValue = "TestValue1505985619";
            this.testClass.ValidatorType = testValue;
            this.testClass.ValidatorType.Should().Be(testValue);
        }

        /// <summary>
        /// Can get and set the target.
        /// </summary>
        [Fact]
        public void CanSetAndGetTarget()
        {
            var testValue = new object();
            this.testClass.Target = testValue;
            this.testClass.Target.Should().Be(testValue);
        }

        /// <summary>
        /// Can get and set the property name.
        /// </summary>
        [Fact]
        public void CanSetAndGetPropertyName()
        {
            var testValue = "TestValue595224707";
            this.testClass.PropertyName = testValue;
            this.testClass.PropertyName.Should().Be(testValue);
        }

        /// <summary>
        /// Can set and get the severity.
        /// </summary>
        [Fact]
        public void CanSetAndGetSeverity()
        {
            var testValue = EventLevel.Informational;
            this.testClass.Severity = testValue;
            this.testClass.Severity.Should().Be(testValue);
        }

        /// <summary>
        /// Can set and get the message.
        /// </summary>
        [Fact]
        public void CanSetAndGetMessage()
        {
            var testValue = "TestValue2070305587";
            this.testClass.Message = testValue;
            this.testClass.Message.Should().Be(testValue);
        }
    }
}