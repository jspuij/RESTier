// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;
using NSubstitute;
using Xunit;

namespace Microsoft.Restier.Tests.Core.Query
{
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
            exception = new Exception();
            results = Substitute.For<IEnumerable>();
            testClass = new QueryResult(results);
        }

        /// <summary>
        /// Can construct the instance.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var instance = new QueryResult(exception);
            instance.Should().NotBeNull();
            instance = new QueryResult(results);
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
            var instance = new QueryResult(exception);
            instance.Exception.Should().Be(exception);
        }

        /// <summary>
        /// Can get and set the exception.
        /// </summary>
        [Fact]
        public void CanSetAndGetException()
        {
            var testValue = new Exception();
            testClass.Exception = testValue;
            testClass.Exception.Should().Be(testValue);
        }

        /// <summary>
        /// Can get and set the results source.
        /// </summary>
        [Fact]
        public void CanSetAndGetResultsSource()
        {
            var testValue = Substitute.For<IEdmEntitySet>();
            testClass.ResultsSource = testValue;
            testClass.ResultsSource.Should().Be(testValue);
        }

        /// <summary>
        /// Results is initialized correctly.
        /// </summary>
        [Fact]
        public void ResultsIsInitializedCorrectly()
        {
            testClass = new QueryResult(results);
            testClass.Results.Should().BeSameAs(results);
        }

        /// <summary>
        /// Can set and get results.
        /// </summary>
        [Fact]
        public void CanSetAndGetResults()
        {
            var testValue = Substitute.For<IEnumerable>();
            testClass.Results = testValue;
            testClass.Results.Should().BeSameAs(testValue);
        }
    }
}