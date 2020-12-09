// <copyright file="ApiBaseTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

#if !NETCOREAPP
namespace Microsoft.Restier.Tests.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.OData.Edm;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Model;
    using Microsoft.Restier.Core.Query;
    using Microsoft.Restier.Tests.Shared;
    using Microsoft.Restier.Tests.Shared.AspNet;
    using Microsoft.Restier.Tests.Shared.Extensions;
    using Xunit;

    /// <summary>
    /// Legacy ApiBase tests.
    /// </summary>
    /// ><remarks>These are unchecked for overlap with the new unit tests.</remarks>
    public class ApiBaseTests
    {
        /// <summary>
        /// A Default ApiBase can be created and this.Disposed.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task DefaultApiBaseCanBeCreatedAndDisposed()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestableEmptyApi, DbContext>(serviceCollection: this.Di);

            Action exceptionTest = () => { api.Dispose(); };
            exceptionTest.Should().NotThrow<Exception>();
        }

        /// <summary>
        /// Tests that GetQueryableSource returns an entityset.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task GetQueryableSource_EntitySet_IsConfiguredCorrectly()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestableEmptyApi, DbContext>(serviceCollection: this.Di) as ApiBase;
            var arguments = new object[0];
            var source = api.GetQueryableSource("Test", arguments);

            this.CheckQueryable(source, typeof(string), new List<string> { "Test" }, arguments);
        }

        /// <summary>
        /// Tests that GetQueryableSource returns an entityset.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task GetQueryableSource_OfT_EntitySet_IsConfiguredCorrectly()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestableEmptyApi, DbContext>(serviceCollection: this.Di) as ApiBase;
            var arguments = new object[0];
            var source = api.GetQueryableSource<string>("Test", arguments);

            this.CheckQueryable(source, typeof(string), new List<string> { "Test" }, arguments);
        }

        /// <summary>
        /// Tests that GetQueryableSource throws if an entityset is not mapped.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task GetQueryableSource_EntitySet_ThrowsIfNotMapped()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestableEmptyApi, DbContext>(serviceCollection: this.DiEmpty) as ApiBase;
            var arguments = new object[0];

            Action exceptionTest = () => { api.GetQueryableSource("Test", arguments); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        /// <summary>
        /// Tests that GetQueryable throws if the container element is the wrong type.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task GetQueryableSource_OfT_ContainerElementThrowsIfWrongType()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestableEmptyApi, DbContext>(serviceCollection: this.Di) as ApiBase;
            var arguments = new object[0];

            Action exceptionTest = () => { api.GetQueryableSource<object>("Test", arguments); };
            exceptionTest.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Tests that GetQueryableSource correctly calls a composable function.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task GetQueryableSource_ComposableFunction_IsConfiguredCorrectly()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestableEmptyApi, DbContext>(serviceCollection: this.Di) as ApiBase;
            var arguments = new object[0];
            var source = api.GetQueryableSource("Namespace", "Function", arguments);

            this.CheckQueryable(source, typeof(DateTime), new List<string> { "Namespace", "Function" }, arguments);
        }

        /// <summary>
        /// Tests that GetQueryableSource correctly calls a composable function.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task GetQueryableSource_OfT_ComposableFunction_IsConfiguredCorrectly()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestableEmptyApi, DbContext>(serviceCollection: this.Di) as ApiBase;
            var arguments = new object[0];
            var source = api.GetQueryableSource<DateTime>("Namespace", "Function", arguments);

            this.CheckQueryable(source, typeof(DateTime), new List<string> { "Namespace", "Function" }, arguments);
        }

        /// <summary>
        /// Tests that GetQueryableSource throws if a composable function is not mapped.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task GetQueryableSource_ComposableFunction_ThrowsIfNotMapped()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestableEmptyApi, DbContext>(serviceCollection: this.DiEmpty) as ApiBase;
            var arguments = new object[0];

            Action exceptionTest = () => { api.GetQueryableSource("Namespace", "Function", arguments); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        /// <summary>
        /// Tests that GetQueryableSource throws if a composable function is not mapped.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task GetQueryableSource_OfT_ComposableFunction_ThrowsIfNotMapped()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestableEmptyApi, DbContext>(serviceCollection: this.DiEmpty) as ApiBase;
            var arguments = new object[0];

            Action exceptionTest = () => { api.GetQueryableSource<DateTime>("Namespace", "Function", arguments); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        /// <summary>
        /// Tests that GetQueryableSource throws if a composable function is of the wrong type.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task GetQueryableSource_ComposableFunction_ThrowsIfWrongType()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestableEmptyApi, DbContext>(serviceCollection: this.Di) as ApiBase;
            var arguments = new object[0];

            Action exceptionTest = () => { api.GetQueryableSource<object>("Namespace", "Function", arguments); };
            exceptionTest.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Tests that QueryAsync with a query returns results.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task QueryAsync_WithQueryReturnsResults()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestableEmptyApi, DbContext>(serviceCollection: this.Di) as ApiBase;

            var request = new QueryRequest(api.GetQueryableSource<string>("Test"));
            var result = await api.QueryAsync(request);
            var results = result.Results.Cast<string>();

            results.SequenceEqual(new[] { "Test" }).Should().BeTrue();
        }

        /// <summary>
        /// Tests that QueryAsync correctly forwards calls.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task QueryAsync_CorrectlyForwardsCall()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestableEmptyApi, DbContext>(serviceCollection: this.Di) as ApiBase;
            var queryRequest = new QueryRequest(api.GetQueryableSource<string>("Test"));
            var queryResult = await api.QueryAsync(queryRequest);

            queryResult.Results.Cast<string>().SequenceEqual(new[] { "Test" }).Should().BeTrue();
        }

        /// <summary>
        /// Tests that SubmitAsync correctly forwards calls.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task SubmitAsync_CorrectlyForwardsCall()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestableEmptyApi, DbContext>(serviceCollection: this.Di) as ApiBase;
            var submitResult = await api.SubmitAsync();

            submitResult.CompletedChangeSet.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that GetQueryableSource does not support enumerating directly.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task GetQueryableSource_CannotEnumerate()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestableEmptyApi, DbContext>(serviceCollection: this.Di) as ApiBase;
            var source = api.GetQueryableSource<string>("Test");

            Action exceptionTest = () => { source.GetEnumerator(); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        /// <summary>
        /// Tests that GetQueryableSource cannot enumerate when cast to IEnumerable.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task GetQueryableSource_CannotEnumerateIEnumerable()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestableEmptyApi, DbContext>(serviceCollection: this.Di) as ApiBase;
            var source = api.GetQueryableSource<string>("Test");

            Action exceptionTest = () => { (source as IEnumerable).GetEnumerator(); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        /// <summary>
        /// Tests that GetQueryableSource cannot execute through its provider.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task GetQueryableSource_ProviderCannotGenericExecute()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestableEmptyApi, DbContext>(serviceCollection: this.Di) as ApiBase;
            var source = api.GetQueryableSource<string>("Test");

            Action exceptionTest = () => { source.Provider.Execute<string>(null); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        /// <summary>
        /// Tests that GetQueryableSource cannot execute through its provider.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task GetQueryableSource_ProviderCannotExecute()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestableEmptyApi, DbContext>(serviceCollection: this.Di) as ApiBase;
            var source = api.GetQueryableSource<string>("Test");

            Action exceptionTest = () => { source.Provider.Execute(null); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        /// <summary>
        /// Runs a set of checks against an IQueryable to make sure it has been processed properly.
        /// </summary>
        /// <param name="source">The <see cref="IQueryable{T}"/> or <see cref="IQueryable"/> to test.</param>
        /// <param name="elementType">The <see cref="Type"/> returned by the <paramref name="source"/>.</param>
        /// <param name="expressionValues">A <see cref="List{T}"/> containing the parts of the expression to check for.</param>
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

        private void Di(IServiceCollection services)
        {
            // services.AddCoreServices(typeof(TestableEmptyApi));
            services.AddChainedService<IModelBuilder>((sp, next) => new TestModelBuilder());
            services.AddChainedService<IModelMapper>((sp, next) => new TestModelMapper());
            services.AddChainedService<IQueryExpressionSourcer>((sp, next) => new TestQuerySourcer());
            this.DiEmpty(services);
        }

        private void DiEmpty(IServiceCollection services)
        {
            services.AddTestDefaultServices();
        }

        private class TestModelBuilder : IModelBuilder
        {
            public Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
            {
                var model = new EdmModel();
                var dummyType = new EdmEntityType("NS", "Dummy");
                model.AddElement(dummyType);
                var container = new EdmEntityContainer("NS", "DefaultContainer");
                container.AddEntitySet("Test", dummyType);
                model.AddElement(container);
                return Task.FromResult((IEdmModel)model);
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
#endif