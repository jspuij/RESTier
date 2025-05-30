// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Query;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using NSubstitute;
using NSubstitute.Core;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.Restier.Tests.AspNetCore.Query;

public class RestierQueryExecutorTests
{
    [Fact]
    public async Task ExecuteQueryAsync_DelegatesToInner()
    {
        // Arrange
        var inner = Substitute.For<IQueryExecutor>();
        var executor = new RestierQueryExecutor { Inner = inner };
        var context = new QueryContext(new TestApi(Substitute.For<IEdmModel>(), Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>()), new QueryRequest(new TestQueryableSource()));
        var query = new[] { 1, 2, 3 }.AsQueryable();
        var cancellationToken = new CancellationToken();
        var expectedResult = new QueryResult(new[] { 1, 2, 3 });

        inner.ExecuteQueryAsync(context, query, cancellationToken)
            .Returns(Task.FromResult(expectedResult));

        // Act
        var result = await executor.ExecuteQueryAsync(context, query, cancellationToken);

        // Assert
        result.Should().BeSameAs(expectedResult);
        await inner.Received(1).ExecuteQueryAsync(context, query, cancellationToken);
    }

    [Fact]
    public async Task ExecuteExpressionAsync_DelegatesToInner()
    {
        // Arrange
        var inner = Substitute.For<IQueryExecutor>();
        var executor = new RestierQueryExecutor { Inner = inner };
        var context = new QueryContext(new TestApi(Substitute.For<IEdmModel>(), Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>()), new QueryRequest(new TestQueryableSource()));
        var provider = Substitute.For<IQueryProvider>();
        var expression = Expression.Constant(42);
        var cancellationToken = new CancellationToken();
        var expectedResult = new QueryResult(new[] { 42 });

        inner.ExecuteExpressionAsync<int>(context, provider, expression, cancellationToken)
            .Returns(Task.FromResult(expectedResult));

        // Act
        var result = await executor.ExecuteExpressionAsync<int>(context, provider, expression, cancellationToken);

        // Assert
        result.Should().BeSameAs(expectedResult);
        await inner.Received(1).ExecuteExpressionAsync<int>(context, provider, expression, cancellationToken);
    }

    [Fact]
    public void Inner_CanBeSetAndGet()
    {
        // Arrange
        var inner = Substitute.For<IQueryExecutor>();
        var executor = new RestierQueryExecutor();

        // Act
        executor.Inner = inner;

        // Assert
        executor.Inner.Should().BeSameAs(inner);
    }

    // TestApi
    public class TestApi : ApiBase
    {
        public TestApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(model, queryHandler, submitHandler)
        {
        }
    }

    internal class TestQueryableSource : QueryableSource
    {
        public TestQueryableSource() : base(Expression.Constant(0))
        {
        }

        public override Type ElementType => typeof(int);
    }
}
