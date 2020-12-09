// <copyright file="QueryRequestTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Tests.Query
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Expressions;
    using FluentAssertions;
    using Microsoft.Restier.Core.Query;
    using Moq;
    using Xunit;

    /// <summary>
    /// Unit tests for the <see cref="QueryRequest"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class QueryRequestTests
    {
        private QueryRequest testClass;
        private IQueryable query = new Mock<IQueryable>().Object;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryRequestTests"/> class.
        /// </summary>
        public QueryRequestTests()
        {
            var query = new QueryableSource<object>(Expression.Constant(this.query));
            this.testClass = new QueryRequest(query);
        }

        /// <summary>
        /// Can construct.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            this.testClass.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot construct with null query.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullQuery()
        {
            Action act = () => new QueryRequest(default(IQueryable));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot construct with non-querysource.
        /// </summary>
        [Fact]
        public void CannotConstructWithNonQuerySource()
        {
            Action act = () => new QueryRequest(this.query);
            act.Should().Throw<NotSupportedException>();
        }

        /// <summary>
        /// Can set and get the expression.
        /// </summary>
        [Fact]
        public void CanSetAndGetExpression()
        {
            var testValue = Expression.Constant(this.query);
            this.testClass.Expression = testValue;
            this.testClass.Expression.Should().Be(testValue);
        }

        /// <summary>
        /// Can set and get ShouldReturnCount.
        /// </summary>
        [Fact]
        public void CanSetAndGetShouldReturnCount()
        {
            var testValue = true;
            this.testClass.ShouldReturnCount = testValue;
            this.testClass.ShouldReturnCount.Should().Be(testValue);
        }
    }
}