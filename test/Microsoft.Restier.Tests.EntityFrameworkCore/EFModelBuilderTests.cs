// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Tests.EntityFrameworkCore.Scenarios.IncorrectLibrary;
using Microsoft.Restier.Tests.EntityFrameworkCore.Scenarios.Views;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Xunit;

namespace Microsoft.Restier.Tests.EntityFrameworkCore;

public class EFModelBuilderTests
{
    [Fact]
    public async Task DbSetOnComplexType_Should_ThrowException()
    {
        var getModelAction = async () =>
        {
            _ = await RestierTestHelpers.GetApiMetadataAsync<IncorrectLibraryApi>(
                serviceCollection: services => services.AddEFCoreProviderServices<IncorrectLibraryContext>((Action<DbContextOptionsBuilder>)null));
        };
        await getModelAction.Should().ThrowAsync<InvalidOperationException>()
            .Where(c => c.ToString().Contains("Address") && c.ToString().Contains("Universe"));
    }

    [Fact]
    public async Task EFModelBuilder_Should_HandleViews()
    {
        var getModelAction = async () =>
        {
            _ = await RestierTestHelpers.GetApiMetadataAsync<LibraryWithViewsApi>(
                serviceCollection: services => services.AddEFCoreProviderServices<LibraryWithViewsContext>((Action<DbContextOptionsBuilder>)null));
        };
        await getModelAction.Should().ThrowAsync<InvalidOperationException>()
            .Where(c => c.ToString().Contains("[Keyless]"));
    }

    [Fact]
    public async Task GetEdmModel_ShouldBuildValidModel_ForStandardContext()
    {
        var metadata = await RestierTestHelpers.GetApiMetadataAsync<LibraryApi>(
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());

        metadata.Should().NotBeNull();
        var metadataString = metadata.ToString();
        metadataString.Should().Contain("Books");
        metadataString.Should().Contain("Publishers");
        metadataString.Should().Contain("Readers");
    }
}
