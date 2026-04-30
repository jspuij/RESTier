// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.NSwag.Extensions
{

    public class IServiceCollectionExtensionsTests
    {

        [Fact]
        public void AddRestierNSwag_NoSettingsAction_RegistersAtLeastOneService()
        {
            var collection = new ServiceCollection();
            collection.AddRestierNSwag();
            collection.Should().NotBeEmpty();
        }

        [Fact]
        public void AddRestierNSwag_WithSettingsAction_RegistersConfiguratorAsSingleton()
        {
            var collection = new ServiceCollection();
            collection.AddRestierNSwag(settings => settings.AddAlternateKeyPaths = true);

            var provider = collection.BuildServiceProvider();
            var configurator = provider.GetService<Action<Microsoft.OpenApi.OData.OpenApiConvertSettings>>();
            configurator.Should().NotBeNull("the settings action must be retrievable as a singleton service");
        }

    }

}
