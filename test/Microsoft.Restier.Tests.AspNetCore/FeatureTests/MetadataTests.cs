// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using CloudNimble.Breakdance.Assemblies;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

[Collection("LibraryApi")]
public class MetadataTests : RestierTestBase<LibraryApi>
{
    private const string RelativePath = "..//..//..//..//Microsoft.Restier.Tests.AspNetCore//";
    private const string BaselineFolder = "Baselines//";

    [Fact]
    public async Task LibraryApi_CompareCurrentApiMetadataToPriorRun()
    {
        var fileName = $"{Path.Combine(RelativePath, BaselineFolder)}{nameof(LibraryApi)}-ApiMetadata.txt";
        File.Exists(fileName).Should().BeTrue();

        var oldReport = File.ReadAllText(fileName);
        var newReport = await RestierTestHelpers.GetApiMetadataAsync<LibraryApi>(
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());

        TraceListener.WriteLine($"Old Report: {oldReport}");
        TraceListener.WriteLine($"New Report: {newReport}");

        oldReport.Should().BeEquivalentTo(newReport.ToString());
    }

    [BreakdanceManifestGenerator]
    private async Task LibraryApi_SaveMetadataDocument(string projectPath)
    {
        await RestierTestHelpers.WriteCurrentApiMetadata<LibraryApi>(
            Path.Combine(projectPath, BaselineFolder),
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
        File.Exists($"{Path.Combine(projectPath, BaselineFolder)}{nameof(LibraryApi)}-ApiMetadata.txt").Should().BeTrue();
    }

    [Fact]
    public async Task MarvelApi_CompareCurrentApiMetadataToPriorRun()
    {
        var fileName = $"{Path.Combine(RelativePath, BaselineFolder)}{nameof(MarvelApi)}-ApiMetadata.txt";
        File.Exists(fileName).Should().BeTrue();

        var oldReport = File.ReadAllText(fileName);
        var newReport = await RestierTestHelpers.GetApiMetadataAsync<MarvelApi>(
            serviceCollection: services => services.AddEntityFrameworkServices<MarvelContext>());

        TraceListener.WriteLine($"Old Report: {oldReport}");
        TraceListener.WriteLine($"New Report: {newReport}");

        oldReport.Should().BeEquivalentTo(newReport.ToString());
    }

    [BreakdanceManifestGenerator]
    private async Task MarvelApi_SaveMetadataDocument(string projectPath)
    {
        await RestierTestHelpers.WriteCurrentApiMetadata<MarvelApi>(
            Path.Combine(projectPath, BaselineFolder),
            serviceCollection: services => services.AddEntityFrameworkServices<MarvelContext>());
        File.Exists($"{Path.Combine(projectPath, BaselineFolder)}{nameof(MarvelApi)}-ApiMetadata.txt").Should().BeTrue();
    }

    [Fact]
    public async Task StoreApi_CompareCurrentApiMetadataToPriorRun()
    {
        var fileName = $"{Path.Combine(RelativePath, BaselineFolder)}{nameof(StoreApi)}-ApiMetadata.txt";
        File.Exists(fileName).Should().BeTrue();

        var oldReport = File.ReadAllText(fileName);
        var newReport = await RestierTestHelpers.GetApiMetadataAsync<StoreApi>();

        TraceListener.WriteLine($"Old Report: {oldReport}");
        TraceListener.WriteLine($"New Report: {newReport}");

        oldReport.Should().BeEquivalentTo(newReport.ToString());
    }

    [BreakdanceManifestGenerator]
    private async Task StoreApi_SaveMetadataDocument(string projectPath)
    {
        await RestierTestHelpers.WriteCurrentApiMetadata<StoreApi>(
            Path.Combine(projectPath, BaselineFolder));
        File.Exists($"{Path.Combine(projectPath, BaselineFolder)}{nameof(StoreApi)}-ApiMetadata.txt").Should().BeTrue();
    }
}
