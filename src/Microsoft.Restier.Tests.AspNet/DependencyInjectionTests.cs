﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Assemblies;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
#if EF6
using Microsoft.Restier.EntityFramework;
#endif
#if EFCore
    using Microsoft.Restier.EntityFrameworkCore;
#endif
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if NET6_0_OR_GREATER
namespace Microsoft.Restier.Tests.AspNetCore
#else
namespace Microsoft.Restier.Tests.AspNet
#endif
{

#if NET6_0_OR_GREATER

    //[TestClass]
    //[TestCategory("Endpoint Routing")]
    //public class DependencyInjectionTests_EndpointRouting : DependencyInjectionTests
    //{
    //    public DependencyInjectionTests_EndpointRouting() : base(true)
    //    {
    //    }
    //}

    [TestClass]
    [TestCategory("Legacy Routing")]
    public class DependencyInjectionTests_LegacyRouting : DependencyInjectionTests
    {
        public DependencyInjectionTests_LegacyRouting() : base(false)
        {
        }
    }

    /// <summary>
    /// Tests Restier's DI construction to ensure consistency between platforms and releases.
    /// </summary>
    [TestClass]
    public abstract class DependencyInjectionTests : RestierTestBase<LibraryApi>
    {

        public DependencyInjectionTests(bool useEndpointRouting) : base(useEndpointRouting)
        {
            //AddRestierAction = builder =>
            //{
            //    builder.AddRestierApi<LibraryApi>(services => services.AddEntityFrameworkServices<LibraryContext>());
            //};
            //MapRestierAction = routeBuilder =>
            //{
            //    routeBuilder.MapApiRoute<LibraryApi>(WebApiConstants.RouteName, WebApiConstants.RoutePrefix, false);
            //};
        }

        //[TestInitialize]
        //public void ClaimsTestSetup() => TestSetup();

#else

    /// <summary>
    /// Tests Restier's DI construction to ensure consistency between platforms and releases.
    /// </summary>
    [TestClass]
    public class DependencyInjectionTests : RestierTestBase
    {

#endif

        [TestMethod]
        public void RestierContainerBuilder_Registered_ShouldHaveServices()
        {
            var container = GetContainerBuilder();
            container.Services.Should().HaveCount(30);
        }

        [Ignore]
        [TestMethod]
        public async Task DI_CompareCurrentVersion_ToRC2()
        {
            var provider = await RestierTestHelpers.GetTestableInjectionContainer<LibraryApi>(serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>(),
                useEndpointRouting: UseEndpointRouting);
            var result = provider.GetContainerContentsLog();
            result.Should().NotBeNullOrEmpty();

            var baseline = File.ReadAllText("..//..//..//..//Microsoft.Restier.Tests.AspNet//Baselines//RC2-LibraryApi-ServiceProvider.txt");
            result.Should().Be(baseline);
        }

        [TestMethod]
        public async Task DI_VerifyModelBuilderInnerHandlers_ToRC2()
        {
            var names = await RestierTestHelpers.GetModelBuilderHierarchy<LibraryApi>(serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>(),
                useEndpointRouting: UseEndpointRouting);
            names.Should().NotBeNull();

            var result = string.Join(Environment.NewLine, names);
            result.Should().NotBeNullOrWhiteSpace();

            //RWM: If we're in a .NET Core test, remove the Core crap.
            result = result.Replace("Core", "");

            var baseline = File.ReadAllText("..//..//..//..//Microsoft.Restier.Tests.AspNet//Baselines/RC2-ModelBuilder-InnerHandlers.txt");
            baseline = baseline.Replace("Model.Restier", "Model.RestierWebApi").Replace("EFModelProducer", typeof(EFModelBuilder).Name);
            result.Should().Be(baseline);
        }

        [BreakdanceManifestGenerator]
        public async Task ContainerContents_WriteOutput(string projectPath)
        {
            //var projectPath = "..//..//..//";
            var provider = await RestierTestHelpers.GetTestableInjectionContainer<LibraryApi>(serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>(),
                useEndpointRouting: UseEndpointRouting);
            var result = provider.GetContainerContentsLog();
            var fullPath = Path.Combine(projectPath, "Baselines//RC6-LibraryApi-ServiceProvider.txt");
            Console.WriteLine(fullPath);

            if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            }
            File.WriteAllText(fullPath, result);
            Console.WriteLine($"File exists: {File.Exists(fullPath)}");
        }

        //[TestMethod]
        [BreakdanceManifestGenerator]
        public async Task IModelBuilder_LogChildren(string projectPath)
        {
            //var projectPath = "..//..//..//";

            var result = await RestierTestHelpers.GetModelBuilderHierarchy<LibraryApi>(serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>(),
                useEndpointRouting: UseEndpointRouting);

            var fullPath = Path.Combine(projectPath, "Baselines//RC6-ModelBuilder-InnerHandlers.txt");
            Console.WriteLine(fullPath);

            if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            }
            File.WriteAllText(fullPath, string.Join(Environment.NewLine, result));
            Console.WriteLine($"File exists: {File.Exists(fullPath)}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private RestierContainerBuilder GetContainerBuilder()
        {
            var container = new RestierContainerBuilder();
            container.Services
                .AddRestierCoreServices()
                .AddRestierConventionBasedServices(typeof(LibraryApi))
                .AddRestierDefaultServices();
            return container;
        }

    }

}