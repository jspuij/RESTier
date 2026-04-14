// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

public class PagingTests : RestierTestBase<LibraryApi>
{
    [Fact]
    public async Task PagingTests_MaxTop()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/Books?$filter=Id in ['c2081e58-21a5-4a15-b0bd-fff03ebadd30','0697576b-d616-4057-9d28-ed359775129e']",
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("Jungle Book, The");
        content.Should().Contain("Color Purple, The");
        content.Should().NotContain("A Clockwork Orange");
    }
}
