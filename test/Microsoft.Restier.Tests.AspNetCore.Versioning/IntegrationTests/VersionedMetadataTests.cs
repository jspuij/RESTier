// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Restier.Tests.AspNetCore.Versioning.Infrastructure;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.IntegrationTests
{

    public class VersionedMetadataTests
    {

        [Fact]
        public async Task GetV1Metadata_ReturnsV1Edm_WithoutAuditLogs()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await VersionedApiFixture.BuildHostAsync(cancellationToken);
            var client = host.GetTestClient();

            var response = await client.GetAsync("/api/v1/$metadata", cancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            body.Should().Contain("EntitySet Name=\"Items\"");
            body.Should().NotContain("EntitySet Name=\"AuditLogs\"",
                "V1 EDM must not surface V2-only entity sets");
        }

        [Fact]
        public async Task GetV2Metadata_ReturnsV2Edm_WithAuditLogs()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await VersionedApiFixture.BuildHostAsync(cancellationToken);
            var client = host.GetTestClient();

            var response = await client.GetAsync("/api/v2/$metadata", cancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            body.Should().Contain("EntitySet Name=\"Items\"");
            body.Should().Contain("EntitySet Name=\"AuditLogs\"");
        }

        [Fact]
        public async Task GetV3_ReturnsNotFound()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await VersionedApiFixture.BuildHostAsync(cancellationToken);
            var client = host.GetTestClient();

            var response = await client.GetAsync("/api/v3/Items", cancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetV1Items_ReturnsOk()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await VersionedApiFixture.BuildHostAsync(cancellationToken);
            var client = host.GetTestClient();

            var response = await client.GetAsync("/api/v1/Items", cancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

    }

}
