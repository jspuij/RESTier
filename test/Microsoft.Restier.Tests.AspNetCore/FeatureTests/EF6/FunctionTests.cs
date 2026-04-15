// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EF6;

[Collection("LibraryApiEF6")]
public class FunctionTests(ITestOutputHelper outputHelper) : FunctionTests<LibraryApi, LibraryContext>(outputHelper)
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();
}
