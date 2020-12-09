// <copyright file="QueryResultTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Tests.Query
{
    using System;
    using System.Collections;
    using System.Diagnostics.CodeAnalysis;
    using FluentAssertions;
    using Microsoft.OData.Edm;
    using Microsoft.Restier.Core.Query;
    using Moq;
    using Xunit;

    /// <summary>
    /// Unit tests for the <see cref="QueryResult"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class QueryResultTests
    {
        private QueryResult testClass;
        private Exception exception;
        private IEnumerable results;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryResultTests"/> class.
        /// </summary>
        public QueryResultTests()
        {
            this.exception = new Exception();
            this.results = new Mock<IEnumerable>().Object;
            this.testClass = new QueryResult(this.results);
        }

        /// <summary>
        /// Can construct the instance.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var instance = new QueryResult(this.exception);
            instance.Should().NotBeNull();
            instance = new QueryResult(this.results);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot construct with a null exception argument.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullException()
        {
            Action act = () => new QueryResult(default(Exception));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot construct with a null results argument.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullResults()
        {
            Action act = () => new QueryResult(default(IEnumerable));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Exception argument is initialized correctly.
        /// </summary>
        [Fact]
        public void ExceptionIsInitializedCorrectly()
        {
            var instance = new QueryResult(this.exception);
            instance.Exception.Should().Be(this.exception);
        }

        /// <summary>
        /// Can get and set the exception.
        /// </summary>
        [Fact]
        public void CanSetAndGetException()
        {
            var testValue = new Exception();
            this.testClass.Exception = testValue;
            this.testClass.Exception.Should().Be(testValue);
        }

        /// <summary>
        /// Can get and set the results source.
        /// </summary>
        [Fact]
        public void CanSetAndGetResultsSource()
        {
            var testValue = new Mock<IEdmEntitySet>().Object;
            this.testClass.ResultsSource = testValue;
            this.testClass.ResultsSource.Should().Be(testValue);
        }

        /// <summary>
        /// Results is initialized correctly.
        /// </summary>
        [Fact]
        public void ResultsIsInitializedCorrectly()
        {
            this.testClass = new QueryResult(this.results);
            this.testClass.Results.Should().BeSameAs(this.results);
        }

        /// <summary>
        /// Can set and get results.
        /// </summary>
        [Fact]
        public void CanSetAndGetResults()
        {
            var testValue = new Mock<IEnumerable>().Object;
            this.testClass.Results = testValue;
            this.testClass.Results.Should().BeSameAs(testValue);
        }
    }
}