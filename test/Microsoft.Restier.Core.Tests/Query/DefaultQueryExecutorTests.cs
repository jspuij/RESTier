// <copyright file="DefaultQueryExecutorTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Tests.Query
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Query;
    using Microsoft.Restier.Tests.Shared;
    using Moq;
    using Xunit;

    /// <summary>
    /// Unit tests for the <see cref="DefaultQueryExecutor"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DefaultQueryExecutorTests : IClassFixture<ServiceProviderFixture>
    {
        private readonly DefaultQueryExecutor testClass;
        private readonly ServiceProviderFixture serviceProviderFixture;
        private readonly IQueryable<Test> queryable = new List<Test>()
        {
            new Test() { Name = "The" },
            new Test() { Name = "Quick" },
            new Test() { Name = "Brown" },
            new Test() { Name = "Fox" },
        }.AsQueryable();

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultQueryExecutorTests"/> class.
        /// </summary>
        /// <param name="serviceProviderFixture">The <see cref="IServiceProvider"/> fixture.</param>
        public DefaultQueryExecutorTests(ServiceProviderFixture serviceProviderFixture)
        {
            this.serviceProviderFixture = serviceProviderFixture;
            this.testClass = new DefaultQueryExecutor();
        }

        /// <summary>
        /// Tests that a new instance can be constructed.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var instance = new DefaultQueryExecutor();
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Can call ExecuteQueryAsync.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CanCallExecuteQueryAsync()
        {
            var context = new QueryContext(
                new TestApi(this.serviceProviderFixture.ServiceProvider),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(this.queryable))));
            var cancellationToken = CancellationToken.None;
            var result = await this.testClass.ExecuteQueryAsync(
                context,
                this.queryable,
                cancellationToken);
            result.Should().NotBeNull();
            result.Results.Should().BeEquivalentTo(this.queryable);
        }

        /// <summary>
        /// Cannot call ExecuteQueryAsync with a null context.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CannotCallExecuteQueryAsyncWithNullContext()
        {
            Func<Task> act = () =>
                this.testClass.ExecuteQueryAsync(
                    default(QueryContext),
                    this.queryable,
                    CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call ExecuteQueryAsync with a null context.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CannotCallExecuteQueryAsyncWithNullQuery()
        {
            var context = new QueryContext(
                new TestApi(this.serviceProviderFixture.ServiceProvider),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(this.queryable))));
            Func<Task> act = () => this.testClass.ExecuteQueryAsync(
                    context,
                    default(IQueryable<Test>),
                    CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Can call ExecuteExpressionAsync.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CanCallExecuteExpressionAsync()
        {
            var context = new QueryContext(
                new TestApi(this.serviceProviderFixture.ServiceProvider),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(this.queryable))));
            var queryProviderMock = new Mock<IQueryProvider>();
            queryProviderMock
                .Setup(x => x.Execute(It.IsAny<Expression>()))
                .Returns<Expression>(ex => Expression.Lambda<Func<IQueryable<Test>>>(ex).Compile()());
            var expression = Expression.Constant(this.queryable);
            var cancellationToken = CancellationToken.None;
            var result = await this.testClass.ExecuteExpressionAsync<Test>(
                context,
                queryProviderMock.Object,
                expression,
                cancellationToken);
            result.Should().NotBeNull();
            ((IEnumerable<object>)result.Results).First().Should().Be(this.queryable);
        }

        /// <summary>
        /// Cannot call ExpressionAsync with a null query provider.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CannotCallExecuteExpressionAsyncWithNullQueryProvider()
        {
            var context = new QueryContext(
                new TestApi(this.serviceProviderFixture.ServiceProvider),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(this.queryable))));
            var expression = Expression.Constant(this.queryable);

            Func<Task> act = () =>
                this.testClass.ExecuteExpressionAsync<Test>(
                    context,
                    default(IQueryProvider),
                    expression,
                    CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call ExecuteExpressionAsync with a null expression.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CannotCallExecuteExpressionAsyncWithNullExpression()
        {
            var context = new QueryContext(
                new TestApi(this.serviceProviderFixture.ServiceProvider),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(this.queryable))));
            var queryProviderMock = new Mock<IQueryProvider>();
            queryProviderMock
                .Setup(x => x.Execute(It.IsAny<Expression>()))
                .Returns<Expression>(ex => Expression.Lambda<Func<IQueryable<Test>>>(ex).Compile()());
            Func<Task> act = () =>
                this.testClass.ExecuteExpressionAsync<Test>(
                    context,
                    queryProviderMock.Object,
                    default(Expression),
                    CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        private class TestApi : ApiBase
        {
            public TestApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }
        }

        private class Test
        {
            public string Name { get; set; }
        }
    }
}