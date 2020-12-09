// <copyright file="QueryContextTests.cs" company="Microsoft Corporation">
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
    using Microsoft.OData.Edm;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Query;
    using Microsoft.Restier.Tests.Shared;
    using Moq;
    using Xunit;

    /// <summary>
    /// Unit tests for the <see cref="QueryContext"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class QueryContextTests : IClassFixture<ServiceProviderFixture>
    {
        private readonly ServiceProviderFixture serviceProviderFixture;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryContextTests"/> class.
        /// </summary>
        /// <param name="serviceProviderFixture">The service provider fixture.</param>
        public QueryContextTests(ServiceProviderFixture serviceProviderFixture)
        {
            this.serviceProviderFixture = serviceProviderFixture;
        }

        /// <summary>
        /// Can construct a new QueryContext.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var api = new TestApi(this.serviceProviderFixture.ServiceProvider);
            var queryableSource = new QueryableSource<Test>(Expression.Constant(new Mock<IQueryable>().Object));
            var request = new QueryRequest(queryableSource);
            var instance = new QueryContext(api, request);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot construct with a null api.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullApi()
        {
            var queryableSource = new QueryableSource<Test>(Expression.Constant(new Mock<IQueryable>().Object));
            var request = new QueryRequest(queryableSource);
            Action act = () => new QueryContext(
                default(ApiBase),
                request);
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot construct with a null request.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullRequest()
        {
            var api = new TestApi(this.serviceProviderFixture.ServiceProvider);
            Action act = () => new QueryContext(api, default(QueryRequest));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Can get and set the model.
        /// </summary>
        [Fact]
        public void CanSetAndGetModel()
        {
            var api = new TestApi(this.serviceProviderFixture.ServiceProvider);
            var queryableSource = new QueryableSource<Test>(Expression.Constant(new Mock<IQueryable>().Object));
            var request = new QueryRequest(queryableSource);
            var instance = new QueryContext(api, request);

            var testValue = new Mock<IEdmModel>().Object;
            instance.Model = testValue;
            instance.Model.Should().Be(testValue);
        }

        /// <summary>
        /// Request is initialized correctly.
        /// </summary>
        [Fact]
        public void RequestIsInitializedCorrectly()
        {
            var api = new TestApi(this.serviceProviderFixture.ServiceProvider);
            var queryableSource = new QueryableSource<Test>(Expression.Constant(new Mock<IQueryable>().Object));
            var request = new QueryRequest(queryableSource);
            var instance = new QueryContext(api, request);

            instance.Request.Should().Be(request);
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