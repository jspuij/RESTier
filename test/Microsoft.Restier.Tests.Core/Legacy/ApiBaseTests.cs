// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using NSubstitute;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.Core
{
    /// <summary>
    /// Unit tests for an <see cref="ApiBase"/> instance.
    /// </summary>
    public partial class ApiBaseTests
    {
        DefaultQueryHandler queryHandler;
        DefaultSubmitHandler submitHandler;
        TestModelBuilder modelBuilder = new TestModelBuilder();

        public ApiBaseTests()
        {
            queryHandler = new DefaultQueryHandler(
                new TestQuerySourcer(),
                new DefaultQueryExecutor(),
                new TestModelMapper(),
                null,
                null,
                new ConventionBasedQueryExpressionProcessor(typeof(EmptyApi))
                );
            submitHandler = new DefaultSubmitHandler(
                new DefaultChangeSetInitializer(),
                new DefaultSubmitExecutor(),
                new ConventionBasedChangeSetItemAuthorizer(typeof(EmptyApi)),
                new ConventionBasedChangeSetItemValidator(),
                new ConventionBasedChangeSetItemFilter(typeof(EmptyApi))
                );
        }

        [Fact]
        public void DefaultApiBaseCanBeCreatedAndDisposed()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);

            Action exceptionTest = () => { api.Dispose(); };
            exceptionTest.Should().NotThrow<Exception>();
        }

        [Fact]
        public void GetQueryableSource_EntitySet_IsConfiguredCorrectly()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var arguments = new object[0];
            var source = api.GetQueryableSource("Test", arguments);

            CheckQueryable(source, typeof(string), new List<string> { "Test" }, arguments);
        }
        [Fact]
        public void GetQueryableSource_OfT_EntitySet_IsConfiguredCorrectly()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var arguments = new object[0];
            var source = api.GetQueryableSource<string>("Test", arguments);

            CheckQueryable(source, typeof(string), new List<string> { "Test" }, arguments);
        }

        [Fact]
        public void GetQueryableSource_EntitySet_ThrowsIfNotMapped()
        {
            queryHandler = new DefaultQueryHandler(
               new TestQuerySourcer(),
               new DefaultQueryExecutor(),
               Substitute.For<IModelMapper>(),
               null,
               null,
               new ConventionBasedQueryExpressionProcessor(typeof(EmptyApi))
               );
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var arguments = new object[0];

            Action exceptionTest = () => { api.GetQueryableSource("Test", arguments); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        [Fact]
        public void GetQueryableSource_OfT_ContainerElementThrowsIfWrongType()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var arguments = new object[0];

            Action exceptionTest = () => { api.GetQueryableSource<object>("Test", arguments); };
            exceptionTest.Should().Throw<ArgumentException>();

        }

        [Fact]
        public void GetQueryableSource_ComposableFunction_IsConfiguredCorrectly()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var arguments = new object[0];
            var source = api.GetQueryableSource("Namespace", "Function", arguments);

            CheckQueryable(source, typeof(DateTime), new List<string> { "Namespace", "Function" }, arguments);
        }

        [Fact]
        public void GetQueryableSource_OfT_ComposableFunction_IsConfiguredCorrectly()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var arguments = new object[0];
            var source = api.GetQueryableSource<DateTime>("Namespace", "Function", arguments);

            CheckQueryable(source, typeof(DateTime), new List<string> { "Namespace", "Function" }, arguments);
        }

        [Fact]
        public void GetQueryableSource_ComposableFunction_ThrowsIfNotMapped()
        {
            queryHandler = new DefaultQueryHandler(
               new TestQuerySourcer(),
               new DefaultQueryExecutor(),
               Substitute.For<IModelMapper>(),
               null,
               null,
               new ConventionBasedQueryExpressionProcessor(typeof(EmptyApi))
               );
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var arguments = new object[0];

            Action exceptionTest = () => { api.GetQueryableSource("Namespace", "Function", arguments); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        [Fact]
        public void GetQueryableSource_OfT_ComposableFunction_ThrowsIfNotMapped()
        {
            queryHandler = new DefaultQueryHandler(
               new TestQuerySourcer(),
               new DefaultQueryExecutor(),
               Substitute.For<IModelMapper>(),
               null,
               null,
               new ConventionBasedQueryExpressionProcessor(typeof(EmptyApi))
               );
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var arguments = new object[0];

            Action exceptionTest = () => { api.GetQueryableSource<DateTime>("Namespace", "Function", arguments); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        [Fact]
        public void GetQueryableSource_ComposableFunction_ThrowsIfWrongType()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var arguments = new object[0];

            Action exceptionTest = () => { api.GetQueryableSource<object>("Namespace", "Function", arguments); };
            exceptionTest.Should().Throw<ArgumentException>();
        }



        [Fact]
        public async Task QueryAsync_WithQueryReturnsResults()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);

            var request = new QueryRequest(api.GetQueryableSource<string>("Test"));
            var result = await api.QueryAsync(request, TestContext.Current.CancellationToken);
            var results = result.Results.Cast<string>();

            results.SequenceEqual(new[] { "Test" }).Should().BeTrue();
        }

        [Fact]
        public async Task QueryAsync_CorrectlyForwardsCall()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var queryRequest = new QueryRequest(api.GetQueryableSource<string>("Test"));
            var queryResult = await api.QueryAsync(queryRequest, TestContext.Current.CancellationToken);

            queryResult.Results.Cast<string>().SequenceEqual(new[] { "Test" }).Should().BeTrue();
        }

        [Fact]
        public async Task SubmitAsync_CorrectlyForwardsCall()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var submitResult = await api.SubmitAsync(cancellationToken: TestContext.Current.CancellationToken);

            submitResult.CompletedChangeSet.Should().NotBeNull();
        }

        [Fact]
        public void GetQueryableSource_CannotEnumerate()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var source = api.GetQueryableSource<string>("Test");

            Action exceptionTest = () => { source.GetEnumerator(); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        [Fact]
        public void GetQueryableSource_CannotEnumerateIEnumerable()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var source = api.GetQueryableSource<string>("Test");

            Action exceptionTest = () => { (source as IEnumerable).GetEnumerator(); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        [Fact]
        public void GetQueryableSource_ProviderCannotGenericExecute()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var source = api.GetQueryableSource<string>("Test");

            Action exceptionTest = () => { source.Provider.Execute<string>(null); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        [Fact]
        public void GetQueryableSource_ProviderCannotExecute()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var source = api.GetQueryableSource<string>("Test");

            Action exceptionTest = () => { source.Provider.Execute(null); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        /// <summary>
        /// Runs a set of checks against an IQueryable to make sure it has been processed properly.
        /// </summary>
        /// <param name="source">The <see cref="IQueryable{T}"/> or <see cref="IQueryable"/> to test.</param>
        /// <param name="elementType">The <see cref="Type"/> returned by the <paramref name="source"/>.</param>
        /// <param name="expressionValues">A <see cref="List{string}"/> containing the parts of the expression to check for.</param>
        /// <param name="arguments">An array of arguments that the <see cref="IQueryable"/> we're testing requires. RWM: In the tests, this is an empty array. Not sure if that is v alid or not.</param>
        private void CheckQueryable(IQueryable source, Type elementType, List<string> expressionValues, object[] arguments)
        {
            source.ElementType.Should().Be(elementType);
            (source.Expression is MethodCallExpression).Should().BeTrue();
            var methodCall = source.Expression as MethodCallExpression;
            methodCall.Object.Should().BeNull();
            methodCall.Method.DeclaringType.Should().Be(typeof(DataSourceStub));
            methodCall.Method.Name.Should().Be("GetQueryableSource");
            methodCall.Method.GetGenericArguments()[0].Should().Be(elementType);
            methodCall.Arguments.Should().HaveCount(expressionValues.Count + 1);

            for (var i = 0; i < expressionValues.Count; i++)
            {
                (methodCall.Arguments[i] is ConstantExpression).Should().BeTrue();
                (methodCall.Arguments[i] as ConstantExpression).Value.Should().Be(expressionValues[i]);
                source.ToString().Should().Be(source.Expression.ToString());
            }

            (methodCall.Arguments[expressionValues.Count] is ConstantExpression).Should().BeTrue();
            (methodCall.Arguments[expressionValues.Count] as ConstantExpression).Value.Should().Be(arguments);
            source.ToString().Should().Be(source.Expression.ToString());

        }

        private class EmptyApi : ApiBase
        {
            public EmptyApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }
        }

        private class TestModelBuilder : IModelBuilder
        {
            public IEdmModel GetEdmModel()
            {
                var model = new EdmModel();
                var dummyType = new EdmEntityType("NS", "Dummy");
                model.AddElement(dummyType);
                var container = new EdmEntityContainer("NS", "DefaultContainer");
                container.AddEntitySet("Test", dummyType);
                model.AddElement(container);
                return model;
            }
        }

        private class TestModelMapper : IModelMapper
        {
            public bool TryGetRelevantType(ModelContext context, string name, out Type relevantType)
            {
                relevantType = typeof(string);
                return true;
            }

            public bool TryGetRelevantType(ModelContext context, string namespaceName, string name, out Type relevantType)
            {
                relevantType = typeof(DateTime);
                return true;
            }
        }

        private class TestQuerySourcer : IQueryExpressionSourcer
        {
            public Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded)
            {
                return Expression.Constant(new[] { "Test" }.AsQueryable());
            }
        }
    }
}