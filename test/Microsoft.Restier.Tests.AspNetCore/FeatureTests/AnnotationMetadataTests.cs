// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Annotated;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

public class AnnotationMetadataTests
{
    private const string RelativePath = "..//..//..//..//Microsoft.Restier.Tests.AspNetCore//";
    private const string BaselineFolder = "Baselines//";

    private static Action<IServiceCollection> ConfigureServices => services =>
    {
        services.AddEFCoreProviderServices<AnnotatedContext>(options =>
            options.UseInMemoryDatabase($"AnnotationTests-{Guid.NewGuid()}"));
    };

    [Fact]
    public async Task AnnotatedApi_MetadataMatchesBaseline()
    {
        var fileName = $"{Path.Combine(RelativePath, BaselineFolder)}{nameof(AnnotatedApi)}-ApiMetadata.txt";
        File.Exists(fileName).Should().BeTrue($"baseline file not found at: {Path.GetFullPath(fileName)}");

        var oldReport = File.ReadAllText(fileName);
        var newReport = await RestierTestHelpers.GetApiMetadataAsync<AnnotatedApi>(
            serviceCollection: ConfigureServices);

        oldReport.Should().BeEquivalentTo(newReport.ToString());
    }
}
