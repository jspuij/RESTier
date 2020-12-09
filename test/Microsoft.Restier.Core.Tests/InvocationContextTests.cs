// <copyright file="InvocationContextTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Tests
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using FluentAssertions;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Query;
    using Microsoft.Restier.Tests.Shared;
    using Moq;
    using Xunit;

    /// <summary>
    /// Unit tests for the <see cref="InvocationContext"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class InvocationContextTests : IClassFixture<ServiceProviderFixture>
    {
        private InvocationContext testClass;
        private ApiBase api;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvocationContextTests"/> class.
        /// </summary>
        /// <param name="serviceProviderFixture">The <see cref="IServiceProvider"/> fixture.</param>
        public InvocationContextTests(ServiceProviderFixture serviceProviderFixture)
        {
            var serviceProvider = serviceProviderFixture.ServiceProvider;
            this.api = new TestApi(serviceProvider);
            this.testClass = new InvocationContext(this.api);
        }

        /// <summary>
        /// Can construct an InvocationContext.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var instance = new InvocationContext(this.api);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot construct an InvocationContext with a null api.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullApi()
        {
            Action act = () => new InvocationContext(default(ApiBase));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Can call GetApiService().
        /// </summary>
        [Fact]
        public void CanCallGetApiService()
        {
            var result = this.testClass.GetApiService<IQueryExecutor>();
            result.Should().NotBeNull();
        }

        /// <summary>
        /// Api is initialized correctly.
        /// </summary>
        [Fact]
        public void ApiIsInitializedCorrectly()
        {
            this.testClass.Api.Should().Be(this.api);
        }

        private class TestApi : ApiBase
        {
            public TestApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }
        }
    }
}