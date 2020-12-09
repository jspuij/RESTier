// <copyright file="QueryExpressionContextTests.cs" company="Microsoft Corporation">
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
    using System.Reflection;
    using FluentAssertions;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Query;
    using Microsoft.Restier.Tests.Shared;
    using Moq;
    using Xunit;

    /// <summary>
    /// Query expression context tests.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class QueryExpressionContextTests : IClassFixture<ServiceProviderFixture>
    {
        private readonly ServiceProviderFixture serviceProviderFixture;
        private readonly QueryExpressionContext testClass;
        private readonly QueryContext queryContext;
        private readonly MethodInfo testGetQuerableSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryExpressionContextTests"/> class.
        /// </summary>
        /// <param name="serviceProviderFixture">The <see cref="IServiceProvider"/> fixture.</param>
        public QueryExpressionContextTests(ServiceProviderFixture serviceProviderFixture)
        {
            this.serviceProviderFixture = serviceProviderFixture;
            var api = new TestApi(this.serviceProviderFixture.ServiceProvider);
            var queryableSource = new QueryableSource<Test>(Expression.Constant(new Mock<IQueryable>().Object));
            var request = new QueryRequest(queryableSource);
            this.queryContext = new QueryContext(api, request);
            this.testClass = new QueryExpressionContext(this.queryContext);
            var type = typeof(DataSourceStub);
            var methodInfo = type.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(x => x.Name == "GetQueryableSource");
            this.testGetQuerableSource = methodInfo.First().MakeGenericMethod(new Type[] { typeof(Test) });
        }

        /// <summary>
        /// Can construct an instance of the <see cref="QueryExpressionContext"/> class.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var instance = new QueryExpressionContext(this.queryContext);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot construct with a null query context.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullQueryContext()
        {
            Action act = () => new QueryExpressionContext(default(QueryContext));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Can call PushVisitedNode.
        /// </summary>
        [Fact]
        public void CanCallPushVisitedNode()
        {
            var visitedNode = Expression.Constant(new Mock<IQueryable>().Object);
            this.testClass.PushVisitedNode(visitedNode);
            this.testClass.VisitedNode.Should().Be(visitedNode);
        }

        /// <summary>
        /// Can call PushVisitedNode and update the model reference.
        /// </summary>
        [Fact]
        public void CanCallPushVisitedNodeAndUpdateModelReference()
        {
            var visitedNode = Expression.Call(this.testGetQuerableSource, new Expression[] { Expression.Constant("Test"), Expression.Constant(new object[0]) });
            this.testClass.PushVisitedNode(visitedNode);
            this.testClass.ModelReference.Should().NotBeNull();
        }

        // TODO: More tests.

        /*



                [Fact]
                public void CanCallReplaceVisitedNode()
                {
                    var visitedNode = new BinaryExpression();
                    testClass.ReplaceVisitedNode(visitedNode);
                    false, "Create or modify test".Should().BeTrue();
                }

                [Fact]
                public void CannotCallReplaceVisitedNodeWithNullVisitedNode()
                {
                    Action act = () => testClass.ReplaceVisitedNode(default(Expression)); act.Should().Throw<ArgumentNullException>();
                }

                [Fact]
                public void CanCallPopVisitedNode()
                {
                    testClass.PopVisitedNode();
                    false, "Create or modify test".Should().BeTrue();
                }

                [Fact]
                public void CanCallGetModelReferenceForNode()
                {
                    var node = new BinaryExpression();
                    var result = testClass.GetModelReferenceForNode(node);
                    false, "Create or modify test".Should().BeTrue();
                }

                [Fact]
                public void CannotCallGetModelReferenceForNodeWithNullNode()
                {
                    Action act = () => testClass.GetModelReferenceForNode(default(Expression)); act.Should().Throw<ArgumentNullException>();
                }

                [Fact]
                public void GetModelReferenceForNodePerformsMapping()
                {
                    var node = new BinaryExpression();
                    var result = testClass.GetModelReferenceForNode(node);
                    result.Type.Should().Be(node.Type);
                }

                [Fact]
                public void QueryContextIsInitializedCorrectly()
                {
                    testClass.QueryContext.Should().Be(queryContext);
                }

                [Fact]
                public void CanGetVisitedNode()
                {
                    testClass.VisitedNode.Should().BeOfType<Expression>();
                    false, "Create or modify test".Should().BeTrue();
                }

                [Fact]
                public void CanGetModelReference()
                {
                    testClass.ModelReference.Should().BeOfType<QueryModelReference>();
                    false, "Create or modify test".Should().BeTrue();
                }

                [Fact]
                public void CanSetAndGetAfterNestedVisitCallback()
                {
                    var testValue = default(Action);
                    testClass.AfterNestedVisitCallback = testValue;
                    testClass.AfterNestedVisitCallback.Should().Be(testValue);
                }
        */
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