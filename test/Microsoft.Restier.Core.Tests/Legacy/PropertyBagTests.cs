// <copyright file="PropertyBagTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

#if !NETCOREAPP
namespace Microsoft.Restier.Tests.Core
{
    using System;
    using System.Data.Entity;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Tests.Shared;
    using Microsoft.Restier.Tests.Shared.AspNet;
    using Microsoft.Restier.Tests.Shared.AspNet.Extensions;
    using Xunit;

    /// <summary>
    /// Legacy propertybag tests.
    /// </summary>
    public class PropertyBagTests
    {
        /// <summary>
        /// PropertyBag manipulates properties correctly.
        /// </summary>
        [Fact]
        public void PropertyBag_ManipulatesPropertiesCorrectly()
        {
            var container = new RestierContainerBuilder();
            container.Services
                .AddRestierCoreServices(typeof(TestableEmptyApi))
                .AddRestierConventionBasedServices(typeof(TestableEmptyApi))
                .AddTestStoreApiServices()
                .AddScoped<MyPropertyBag>();
            var provider = container.BuildContainer();
            var api = provider.GetService<TestableEmptyApi>();

            api.HasProperty("Test").Should().BeFalse();
            api.GetProperty("Test").Should().BeNull();
            api.GetProperty<string>("Test").Should().BeNull();
            api.GetProperty<int>("Test").Should().Be(default);

            api.SetProperty("Test", "Test");
            api.HasProperty("Test").Should().BeTrue();
            api.GetProperty("Test").Should().Be("Test");
            api.GetProperty<string>("Test").Should().Be("Test");

            api.RemoveProperty("Test");
            api.HasProperty("Test").Should().BeFalse();
            api.GetProperty("Test").Should().BeNull();
            api.GetProperty<string>("Test").Should().BeNull();
            api.GetProperty<int>("Test").Should().Be(default);
        }

        /// <summary>
        /// PropertyBag instances do not conflict.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task PropertyBag_InstancesDoNotConflict()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestableEmptyApi, DbContext>();

            api.SetProperty("Test", 2);
            api.GetProperty<int>("Test").Should().Be(2);
        }

        /// <summary>
        /// PropertyBags are disposed correctly.
        /// </summary>
        [Fact]
        public void PropertyBagsAreDisposedCorrectly()
        {
            var container = new RestierContainerBuilder();
            container.Services
                .AddRestierCoreServices(typeof(TestableEmptyApi))
                .AddRestierConventionBasedServices(typeof(TestableEmptyApi))
                .AddTestStoreApiServices()
                .AddScoped<MyPropertyBag>();

            var provider = container.BuildContainer();
            var scope = provider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var scopedProvider = scope.ServiceProvider;
            var api = scopedProvider.GetService<ApiBase>();

            api.GetApiService<MyPropertyBag>().Should().NotBeNull();
            MyPropertyBag.InstanceCount.Should().Be(1);

            var scopedProvider2 = provider.GetRequiredService<IServiceScopeFactory>().CreateScope().ServiceProvider;
            var api2 = scopedProvider2.GetService<ApiBase>();

            api2.GetApiService<MyPropertyBag>().Should().NotBeNull();
            MyPropertyBag.InstanceCount.Should().Be(2);

            scope.Dispose();

            MyPropertyBag.InstanceCount.Should().Be(1);
        }

        /// <summary>
        /// <see cref="MyPropertyBag"/> has the same lifetime as PropertyBag thus
        /// use this class to test the lifetime of PropertyBag in ApiConfiguration
        /// and ApiBase.
        /// </summary>
        private class MyPropertyBag : IDisposable
        {
            public MyPropertyBag()
            {
                ++InstanceCount;
            }

            public static int InstanceCount { get; set; }

            public void Dispose()
            {
                --InstanceCount;
            }
        }
    }
}
#endif
