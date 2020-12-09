// <copyright file="RestierContainerBuilderTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Tests
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using FluentAssertions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.OData;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Tests.Shared;
    using Xunit;

    /// <summary>
    /// Unit tests for the <see cref="RestierContainerBuilder"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class RestierContainerBuilderTests : IClassFixture<ServiceProviderFixture>
    {
        private readonly ServiceProviderFixture serviceProviderFixture;
        private RestierContainerBuilder testClass;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierContainerBuilderTests"/> class.
        /// </summary>
        /// <param name="serviceProviderFixture">Service provider fixture.</param>
        public RestierContainerBuilderTests(ServiceProviderFixture serviceProviderFixture)
        {
            this.serviceProviderFixture = serviceProviderFixture;
            this.testClass = new RestierContainerBuilder(s =>
            {
            });
        }

        /// <summary>
        /// Can construct a new instance of the <see cref="RestierContainerBuilder"/> class.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var apiType = typeof(TestApi);
            var instance = new RestierContainerBuilder(s => { });
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Can call AddService with a lifetype and a service type and an implementation type.
        /// </summary>
        [Fact]
        public void CanCallAddServiceWithSingletonifetimeAndServiceTypeAndImplementationType()
        {
            var lifetime = OData.ServiceLifetime.Singleton;
            var serviceType = typeof(ApiBase);
            var implementationType = typeof(TestApi);
            var result = this.testClass.AddService(lifetime, serviceType, implementationType);
            this.testClass.Services.Should().Contain(s => s.ServiceType == serviceType && s.ImplementationType == implementationType && s.Lifetime == Extensions.DependencyInjection.ServiceLifetime.Singleton);
        }

        /// <summary>
        /// Can call AddService with a lifetype and a service type and an implementation type.
        /// </summary>
        [Fact]
        public void CanCallAddServiceWithScopedLifetimeAndServiceTypeAndImplementationType()
        {
            var lifetime = OData.ServiceLifetime.Scoped;
            var serviceType = typeof(ApiBase);
            var implementationType = typeof(TestApi);
            var result = this.testClass.AddService(lifetime, serviceType, implementationType);
            this.testClass.Services.Should().Contain(s => s.ServiceType == serviceType && s.ImplementationType == implementationType && s.Lifetime == Extensions.DependencyInjection.ServiceLifetime.Scoped);
        }

        /// <summary>
        /// Can call AddService with a lifetype and a service type and an implementation type.
        /// </summary>
        [Fact]
        public void CanCallAddServiceWithTransientLifetimeAndServiceTypeAndImplementationType()
        {
            var lifetime = OData.ServiceLifetime.Transient;
            var serviceType = typeof(ApiBase);
            var implementationType = typeof(TestApi);
            var result = this.testClass.AddService(lifetime, serviceType, implementationType);
            this.testClass.Services.Should().Contain(s => s.ServiceType == serviceType && s.ImplementationType == implementationType && s.Lifetime == Extensions.DependencyInjection.ServiceLifetime.Transient);
        }

        /// <summary>
        /// Cannot call AddService with a null servicetype.
        /// </summary>
        [Fact]
        public void CannotCallAddServiceWithLifetimeAndServiceTypeAndImplementationTypeWithNullServiceType()
        {
            Action act = () => this.testClass.AddService(OData.ServiceLifetime.Scoped, default(Type), typeof(TestApi));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call AddService with a null implementation type.
        /// </summary>
        [Fact]
        public void CannotCallAddServiceWithLifetimeAndServiceTypeAndImplementationTypeWithNullImplementationType()
        {
            Action act = () => this.testClass.AddService(OData.ServiceLifetime.Scoped, typeof(ApiBase), default(Type));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Can call AddService with a lifetype and a service type and an implementation type.
        /// </summary>
        [Fact]
        public void CanCallAddServiceWithSingletonifetimeAndServiceTypeAndImplementationFactory()
        {
            var lifetime = OData.ServiceLifetime.Singleton;
            var serviceType = typeof(ApiBase);
            Func<IServiceProvider, object> implementationFactory = s => new TestApi(this.serviceProviderFixture.ServiceProvider);
            var result = this.testClass.AddService(lifetime, serviceType, implementationFactory);
            this.testClass.Services.Should().Contain(s => s.ServiceType == serviceType && s.ImplementationFactory == implementationFactory && s.Lifetime == Extensions.DependencyInjection.ServiceLifetime.Singleton);
        }

        /// <summary>
        /// Can call AddService with a lifetype and a service type and an implementation type.
        /// </summary>
        [Fact]
        public void CanCallAddServiceWithScopedLifetimeAndServiceTypeAndImplementationFactory()
        {
            var lifetime = OData.ServiceLifetime.Scoped;
            var serviceType = typeof(ApiBase);
            Func<IServiceProvider, object> implementationFactory = s => new TestApi(this.serviceProviderFixture.ServiceProvider);
            var result = this.testClass.AddService(lifetime, serviceType, implementationFactory);
            this.testClass.Services.Should().Contain(s => s.ServiceType == serviceType && s.ImplementationFactory == implementationFactory && s.Lifetime == Extensions.DependencyInjection.ServiceLifetime.Scoped);
        }

        /// <summary>
        /// Can call AddService with a lifetype and a service type and an implementation type.
        /// </summary>
        [Fact]
        public void CanCallAddServiceWithTransientLifetimeAndServiceTypeAndImplementationFactory()
        {
            var lifetime = OData.ServiceLifetime.Transient;
            var serviceType = typeof(ApiBase);
            Func<IServiceProvider, object> implementationFactory = s => new TestApi(this.serviceProviderFixture.ServiceProvider);
            var result = this.testClass.AddService(lifetime, serviceType, implementationFactory);
            this.testClass.Services.Should().Contain(s => s.ServiceType == serviceType && s.ImplementationFactory == implementationFactory && s.Lifetime == Extensions.DependencyInjection.ServiceLifetime.Transient);
        }

        /// <summary>
        /// Cannot call AddService with a null service type.
        /// </summary>
        [Fact]
        public void CannotCallAddServiceWithLifetimeAndServiceTypeAndImplementationFactoryWithNullServiceType()
        {
            Action act = () => this.testClass.AddService(OData.ServiceLifetime.Transient, default(Type), default(Func<IServiceProvider, object>));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call AddSErvice with a null implementationfactory.
        /// </summary>
        [Fact]
        public void CannotCallAddServiceWithLifetimeAndServiceTypeAndImplementationFactoryWithNullImplementationFactory()
        {
            Action act = () => this.testClass.AddService(OData.ServiceLifetime.Scoped, typeof(TestApi), default(Func<IServiceProvider, object>));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Can call BuildContainer.
        /// </summary>
        [Fact]
        public void CanCallBuildContainer()
        {
            var result = this.testClass.BuildContainer();
            result.Should().NotBeNull();
        }

        /// <summary>
        /// Can call GetServices.
        /// </summary>
        [Fact]
        public void CanGetServices()
        {
            this.testClass.Services.Should().BeOfType<ServiceCollection>();
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