// <copyright file="MetadataTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.AspNet.Tests.FeatureTests
{
    using System.Data.Entity;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Restier.Tests.Shared;
    using Microsoft.Restier.Tests.Shared.AspNet;
    using Microsoft.Restier.Tests.Shared.AspNet.Extensions;
    using Microsoft.Restier.Tests.Shared.AspNet.Scenarios.Library;
    using Microsoft.Restier.Tests.Shared.EntityFramework.Scenarios.Library;
    using Microsoft.Restier.Tests.Shared.Extensions;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Metadata unit tests.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class MetadataTests
    {
        private const string RelativePath = "..//..//..//Baselines//";

        private readonly ITestOutputHelper output;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataTests"/> class.
        /// </summary>
        /// <param name="output">The helper to output into during the tests.</param>
        public MetadataTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Tests saving the metadata document.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task LibraryApi_SaveMetadataDocument()
        {
            await RestierTestHelpers.WriteCurrentApiMetadata<LibraryApi, LibraryContext>(RelativePath);
            File.Exists($"{RelativePath}{typeof(LibraryApi).Name}-ApiMetadata.txt").Should().BeTrue();
        }

        /// <summary>
        /// Saves the visibility matrix.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task LibraryApi_SaveVisibilityMatrix()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<LibraryApi, LibraryContext>();
            await api.WriteCurrentVisibilityMatrix(RelativePath);

            File.Exists($"{RelativePath}{api.GetType().Name}-ApiSurface.txt").Should().BeTrue();
        }

        /// <summary>
        /// Compares the current Api Metadata to a prior run.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task LibraryApi_CompareCurrentApiMetadataToPriorRun()
        {
            var fileName = $"{RelativePath}{typeof(LibraryApi).Name}-ApiMetadata.txt";
            File.Exists(fileName).Should().BeTrue();

            var oldReport = File.ReadAllText(fileName);
            var newReport = await RestierTestHelpers.GetApiMetadata<LibraryApi, LibraryContext>();
            newReport.ToString().Should().Be(oldReport);
        }

        /// <summary>
        /// Compares the current visibility matrix to a previous run.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task LibraryApi_CompareCurrentVisibilityMatrixToPriorRun()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<LibraryApi, LibraryContext>();
            var fileName = $"{RelativePath}{api.GetType().Name}-ApiSurface.txt";

            File.Exists(fileName).Should().BeTrue();
            var oldReport = File.ReadAllText(fileName);
            var newReport = await api.GenerateVisibilityMatrix();
            newReport.Should().Be(oldReport);
        }

        /// <summary>
        /// Tests saving of the metadata document for the Store Api.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task StoreApi_SaveMetadataDocument()
        {
            await RestierTestHelpers.WriteCurrentApiMetadata<StoreApi, DbContext>(RelativePath, serviceCollection: (services) => { services.AddTestStoreApiServices(); });
            File.Exists($"{RelativePath}{typeof(StoreApi).Name}-ApiMetadata.txt").Should().BeTrue();
        }

        /// <summary>
        /// Store api stest saving the the visibility matrix.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task StoreApi_SaveVisibilityMatrix()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<StoreApi, DbContext>(serviceCollection: (services) => { services.AddTestStoreApiServices(); });
            await api.WriteCurrentVisibilityMatrix(RelativePath);

            File.Exists($"{RelativePath}{api.GetType().Name}-ApiSurface.txt").Should().BeTrue();
        }

        /// <summary>
        /// Store api compare current api to previous run.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task StoreApi_CompareCurrentApiMetadataToPriorRun()
        {
            var fileName = $"{RelativePath}{typeof(StoreApi).Name}-ApiMetadata.txt";
            File.Exists(fileName).Should().BeTrue();

            var oldReport = File.ReadAllText(fileName);
            var newReport = await RestierTestHelpers.GetApiMetadata<StoreApi, DbContext>(serviceCollection: (services) => { services.AddTestStoreApiServices(); });
            newReport.ToString().Should().Be(oldReport);
        }

        /// <summary>
        /// Store api compare current visibility matrix to a prior run.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task StoreApi_CompareCurrentVisibilityMatrixToPriorRun()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<StoreApi, DbContext>(serviceCollection: (services) => { services.AddTestStoreApiServices(); });
            var fileName = $"{RelativePath}{api.GetType().Name}-ApiSurface.txt";

            File.Exists(fileName).Should().BeTrue();
            var oldReport = File.ReadAllText(fileName);
            var newReport = await api.GenerateVisibilityMatrix();
            newReport.Should().Be(oldReport);
        }
    }
}