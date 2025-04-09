// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Restier.Core.Submit;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using Xunit;

namespace Microsoft.Restier.Tests.Core.Submit
{

    /// <summary>
    /// Unit tests for the <see cref="ChangeSetItemValidationResult"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ChangeSetItemValidationResultTests
    {
        private readonly ChangeSetItemValidationResult testClass;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeSetItemValidationResultTests"/> class.
        /// </summary>
        public ChangeSetItemValidationResultTests()
        {
            testClass = new ChangeSetItemValidationResult();
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
            testClass.Message = "Lorem ipsum";
            var result = testClass.ToString();
            result.Should().Be(testClass.Message);
        }

        /// <summary>
        /// Can get and set the Validator type.
        /// </summary>
        [Fact]
        public void CanSetAndGetValidatorType()
        {
            var testValue = "TestValue1505985619";
            testClass.ValidatorType = testValue;
            testClass.ValidatorType.Should().Be(testValue);
        }

        /// <summary>
        /// Can get and set the target.
        /// </summary>
        [Fact]
        public void CanSetAndGetTarget()
        {
            var testValue = new object();
            testClass.Target = testValue;
            testClass.Target.Should().Be(testValue);
        }

        /// <summary>
        /// Can get and set the property name.
        /// </summary>
        [Fact]
        public void CanSetAndGetPropertyName()
        {
            var testValue = "TestValue595224707";
            testClass.PropertyName = testValue;
            testClass.PropertyName.Should().Be(testValue);
        }

        /// <summary>
        /// Can set and get the severity.
        /// </summary>
        [Fact]
        public void CanSetAndGetSeverity()
        {
            var testValue = EventLevel.Informational;
            testClass.Severity = testValue;
            testClass.Severity.Should().Be(testValue);
        }

        /// <summary>
        /// Can set and get the message.
        /// </summary>
        [Fact]
        public void CanSetAndGetMessage()
        {
            var testValue = "TestValue2070305587";
            testClass.Message = testValue;
            testClass.Message.Should().Be(testValue);
        }
    }
}