// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

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

    }

}
