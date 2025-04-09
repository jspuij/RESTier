// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using NSubstitute;
using Xunit;

namespace Microsoft.Restier.Tests.Core
{
    /// <summary>
    /// Unit tests for the <see cref="ConventionBasedQueryExpressionProcessor"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ConventionBasedQueryExpressionProcessorTests
    {
        private readonly IQueryHandler queryHandler;
        private readonly IEdmModel model;
        private readonly ISubmitHandler submitHandler;
        private readonly TestTraceListener testTraceListener = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConventionBasedQueryExpressionProcessorTests"/> class.
        /// </summary>
        public ConventionBasedQueryExpressionProcessorTests()
        {
            queryHandler = Substitute.For<IQueryHandler>();
            model = Substitute.For<IEdmModel>();
            submitHandler = Substitute.For<ISubmitHandler>();

            Trace.Listeners.Add(testTraceListener);
        }

        /// <summary>
        /// Checks that we can construct the <see cref="ConventionBasedQueryExpressionProcessor"/> class.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var instance = new ConventionBasedQueryExpressionProcessor(typeof(EmptyApi));
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Checks that we cannot construct ConventionBasedQueryExpressionProcessor with a null api type.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullTargetType()
        {
            Action act = () => new ConventionBasedQueryExpressionProcessor(default(Type));
            act.Should().Throw<ArgumentNullException>();
        }

        // TODO: more testing.
        /*
                [Fact]
                public void CanCallProcess()
                {
                    var context = new QueryExpressionContext(new QueryContext(new ApiBase(new Mock<IServiceProvider>().Object), new QueryRequest(new Mock<IQueryable>().Object)));
                    var result = _testClass.Process(context);
                    false, "Create or modify test".Should().BeTrue();
                }
        */

        /// <summary>
        /// Checks that processing by the inner processor will bypass the current one.
        /// </summary>
        [Fact]
        public void InnerProcessorShortCircuits()
        {
            queryHandler.EnsureElementType(Arg.Any<ModelContext>(), null, "Tests").Returns(typeof(Test));
            var api = new QueryFilterApi(model, queryHandler, submitHandler);
            var instance = new ConventionBasedQueryExpressionProcessor(typeof(EmptyApi));
            var queryable = api.GetQueryableSource("Tests");
            var queryRequest = new QueryRequest(queryable);
            var queryContext = new QueryContext(api, queryRequest);
            var queryExpressionContext = new QueryExpressionContext(queryContext);
            var processor = Substitute.For<IQueryExpressionProcessor>();
            var expression = Expression.Constant(42);
            processor.Process(queryExpressionContext).Returns(expression);
            instance.Inner = processor;

            var result = instance.Process(queryExpressionContext);

            result.Should().Be(expression);
        }

        // TODO: More tests.

        /// <summary>
        /// Cannot call the Process method with a null context.
        /// </summary>
        [Fact]
        public void CannotCallProcessWithNullContext()
        {
            var instance = new ConventionBasedQueryExpressionProcessor(typeof(EmptyApi));
            Action act = () => instance.Process(default(QueryExpressionContext));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Can get and set the Inner property.
        /// </summary>
        [Fact]
        public void CanSetAndGetInner()
        {
            var instance = new ConventionBasedQueryExpressionProcessor(typeof(EmptyApi));
            var testValue = Substitute.For<IQueryExpressionProcessor>();
            instance.Inner = testValue;
            instance.Inner.Should().Be(testValue);
        }

        private class EmptyApi : ApiBase
        {
            public EmptyApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }
        }

        private class QueryFilterApi : ApiBase
        {
            public QueryFilterApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }
        }

        private class Test
        {
        }
    }
}