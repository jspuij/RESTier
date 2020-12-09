// <copyright file="ModelContextTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using FluentAssertions;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Model;
    using Microsoft.Restier.Tests.Shared;
    using Moq;
    using Xunit;

    /// <summary>
    /// Unit tests for the <see cref="ModelContext"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ModelContextTests : IClassFixture<ServiceProviderFixture>
    {
        private ModelContext testClass;
        private ApiBase api;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelContextTests"/> class.
        /// </summary>
        /// <param name="serviceProviderFixture">Fixture for <see cref="IServiceProvider"/> instance.</param>
        public ModelContextTests(ServiceProviderFixture serviceProviderFixture)
        {
            var serviceProvider = serviceProviderFixture.ServiceProvider;
            this.api = new TestApi(serviceProvider);
            this.testClass = new ModelContext(this.api);
        }

        /// <summary>
        /// Tests that a model context can be constructed.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var instance = new ModelContext(this.api);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that a model context cannot be constructed without an ApiBase.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullApi()
        {
            Action act = () => new ModelContext(default(ApiBase));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Tests that the ResourceMap can be retrieved.
        /// </summary>
        [Fact]
        public void CanGetResourceSetTypeMap()
        {
            this.testClass.ResourceSetTypeMap.Should().BeAssignableTo<IDictionary<string, Type>>();
        }

        /// <summary>
        /// Tests that the ResourceTypeKeyPropertiesMap can be retreived.
        /// </summary>
        [Fact]
        public void CanGetResourceTypeKeyPropertiesMap()
        {
            this.testClass.ResourceTypeKeyPropertiesMap.Should().BeAssignableTo<IDictionary<Type, ICollection<PropertyInfo>>>();
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