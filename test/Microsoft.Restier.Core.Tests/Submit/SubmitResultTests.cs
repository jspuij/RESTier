// <copyright file="SubmitResultTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Tests.Submit
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using FluentAssertions;
    using Microsoft.Restier.Core.Submit;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="SubmitResult"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class SubmitResultTests
    {
        private SubmitResult testClass;
        private Exception exception;
        private ChangeSet completedChangeSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubmitResultTests"/> class.
        /// </summary>
        public SubmitResultTests()
        {
            this.exception = new Exception();
            this.completedChangeSet = new ChangeSet();
            this.testClass = new SubmitResult(this.exception);
        }

        /// <summary>
        /// Can construct a new Submit result.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var instance = new SubmitResult(this.exception);
            instance.Should().NotBeNull();
            instance = new SubmitResult(this.completedChangeSet);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot construct with a null exception.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullException()
        {
            Action act = () => new SubmitResult(default(Exception));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot construct with a null completed changeset.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullCompletedChangeSet()
        {
            Action act = () => new SubmitResult(default(ChangeSet));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Exception is initialized correctly.
        /// </summary>
        [Fact]
        public void ExceptionIsInitializedCorrectly()
        {
            this.testClass.Exception.Should().Be(this.exception);
        }

        /// <summary>
        /// Can get and set Exception.
        /// </summary>
        [Fact]
        public void CanSetAndGetException()
        {
            var testValue = new Exception();
            this.testClass.Exception = testValue;
            this.testClass.Exception.Should().Be(testValue);
        }

        /// <summary>
        /// Setting the exception resets the completed changeset.
        /// </summary>
        [Fact]
        public void ExceptionResetsCompletedChangeSet()
        {
            this.testClass.CompletedChangeSet = new ChangeSet();
            var testValue = new Exception();
            this.testClass.Exception = testValue;
            this.testClass.CompletedChangeSet.Should().BeNull();
        }

        /// <summary>
        /// CompletedChangeSet is initialized.
        /// </summary>
        [Fact]
        public void CompletedChangeSetIsInitializedCorrectly()
        {
            this.testClass = new SubmitResult(this.completedChangeSet);
            this.testClass.CompletedChangeSet.Should().Be(this.completedChangeSet);
        }

        /// <summary>
        /// Can get and set completed Changeset.
        /// </summary>
        [Fact]
        public void CanSetAndGetCompletedChangeSet()
        {
            var testValue = new ChangeSet();
            this.testClass.CompletedChangeSet = testValue;
            this.testClass.CompletedChangeSet.Should().Be(testValue);
        }

        /// <summary>
        /// Setting the completed changeset resets the Exception.
        /// </summary>
        [Fact]
        public void CompletedChangeSetResetsException()
        {
            var testValue = new Exception();
            this.testClass.Exception = testValue;
            this.testClass.CompletedChangeSet = new ChangeSet();
            this.testClass.Exception.Should().BeNull();
        }
    }
}