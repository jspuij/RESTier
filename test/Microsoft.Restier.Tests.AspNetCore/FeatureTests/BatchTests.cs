// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using CloudNimble.Breakdance.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

public abstract class BatchTests<TApi, TContext> : RestierTestBase<TApi> where TApi : ApiBase where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    protected abstract Task CleanupBatchBooksAsync();

    [Fact]
    public async Task BatchTests_AddMultipleEntries()
    {
        await CleanupBatchBooksAsync();

        try
        {
            var client = await GetHttpClientAsync();
            using var request = new HttpRequestMessage(HttpMethod.Post, "$batch")
            {
                Content = new StringContent(MimeBatchRequest, Encoding.UTF8),
            };
            request.Content.Headers.ContentType = MediaTypeWithQualityHeaderValue.Parse("multipart/mixed;boundary=batch_2e6281b5-fc5f-47c1-9692-5ad43fa6088b");

            var batchResponse = await client.SendAsync(request, Xunit.TestContext.Current.CancellationToken);
            _ = await TraceListener.LogAndReturnMessageContentAsync(batchResponse);
            batchResponse.IsSuccessStatusCode.Should().BeTrue();

            var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
                HttpMethod.Get,
                resource: "/Books?$expand=Publisher",
                serviceCollection: ConfigureServices);
            var content = await TraceListener.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("1111111111111");
            content.Should().Contain("2222222222222");
        }
        finally
        {
            await CleanupBatchBooksAsync();
        }
    }

    [Fact]
    public async Task BatchTests_MimePayloadTest()
    {
        await CleanupBatchBooksAsync();

        try
        {
            var client = await GetHttpClientAsync();
            using var request = new HttpRequestMessage(HttpMethod.Post, "$batch")
            {
                Content = new StringContent(MimeBatchRequest, Encoding.UTF8),
            };
            request.Content.Headers.ContentType = MediaTypeWithQualityHeaderValue.Parse("multipart/mixed;boundary=batch_2e6281b5-fc5f-47c1-9692-5ad43fa6088b");

            var response = await client.SendAsync(request, Xunit.TestContext.Current.CancellationToken);
            var content = await TraceListener.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            // Normalize line endings: MIME responses use \r\n but verbatim string constants use \n on Unix.
            var normalizedContent = content.Replace("\r\n", "\n");
            normalizedContent.Should().Contain(BatchResponse1);
            normalizedContent.Should().Contain(BatchResponse2);
        }
        finally
        {
            await CleanupBatchBooksAsync();
        }
    }

    [Fact]
    public async Task BatchTests_JsonPayloadTest()
    {
        await CleanupBatchBooksAsync();

        try
        {
            var client = await GetHttpClientAsync();
            using var request = new HttpRequestMessage(HttpMethod.Post, "$batch")
            {
                Content = new StringContent(JsonBatchRequest, Encoding.UTF8),
            };
            request.Content.Headers.ContentType = MediaTypeWithQualityHeaderValue.Parse("application/json");

            var response = await client.SendAsync(request, Xunit.TestContext.Current.CancellationToken);
            var content = await TraceListener.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Be(JsonBatchResponse);
        }
        finally
        {
            await CleanupBatchBooksAsync();
        }
    }

    [Fact]
    public async Task BatchTests_SelectPlusFunctionResult()
    {
        var client = await GetHttpClientAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, "$batch")
        {
            Content = new StringContent(SelectPlusFunctionBatchRequest, Encoding.UTF8),
        };
        request.Content.Headers.ContentType = MediaTypeWithQualityHeaderValue.Parse("application/json");

        var response = await client.SendAsync(request, Xunit.TestContext.Current.CancellationToken);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("Publisher1");
        content.Should().Contain("The Cat in the Hat");
    }

    private async Task<HttpClient> GetHttpClientAsync()
    {
        var httpClient = await RestierTestHelpers.GetTestableHttpClient<TApi>(
            serviceCollection: ConfigureServices);
        httpClient.BaseAddress = new Uri($"{WebApiConstants.Localhost}{WebApiConstants.RoutePrefix}");
        return httpClient;
    }

    private const string MimeBatchRequest =
@"--batch_2e6281b5-fc5f-47c1-9692-5ad43fa6088b
Content-Type: multipart/mixed;boundary=changeset_ee671721-3d96-462d-ac58-67530e4b530c

--changeset_ee671721-3d96-462d-ac58-67530e4b530c
Content-Type: application/http
Content-Transfer-Encoding: binary
Content-ID: 1

POST http://localhost/api/tests/Books HTTP/1.1
Content-ID: 1
Prefer: return=representation
OData-Version: 4.0
Content-Type: application/json;odata.metadata=minimal;odata.streaming=true;IEEE754Compatible=false;charset=utf-8

{""@odata.type"":""#Microsoft.Restier.Tests.Shared.Scenarios.Library.Book"",""Id"":""79874b37-ce46-4f4c-aa74-8e02ce4d8b67"",""Isbn"":""1111111111111"",""Title"":""Batch Test #1"",""IsActive"":true,""Publisher@odata.bind"":""http://localhost/api/tests/Publishers(%27Publisher1%27)""}
--changeset_ee671721-3d96-462d-ac58-67530e4b530c
Content-Type: application/http
Content-Transfer-Encoding: binary
Content-ID: 2

POST http://localhost/api/tests/Books HTTP/1.1
Content-ID: 2
Prefer: return=representation
OData-Version: 4.0
Content-Type: application/json;odata.metadata=minimal;odata.streaming=true;IEEE754Compatible=false;charset=utf-8

{""@odata.type"":""#Microsoft.Restier.Tests.Shared.Scenarios.Library.Book"",""Id"":""c6b67ec7-badc-45c6-98c7-c76b570ce694"",""Isbn"":""2222222222222"",""Title"":""Batch Test #2"",""IsActive"":true,""Publisher@odata.bind"":""http://localhost/api/tests/Publishers(%27Publisher1%27)""}
--changeset_ee671721-3d96-462d-ac58-67530e4b530c--
--batch_2e6281b5-fc5f-47c1-9692-5ad43fa6088b--
";

    private const string BatchResponse1 =
@"Content-Type: application/http
Content-Transfer-Encoding: binary
Content-ID: 1

HTTP/1.1 201 Created
Location: http://localhost/api/tests/Books(79874b37-ce46-4f4c-aa74-8e02ce4d8b67)
Content-Type: application/json; odata.metadata=minimal; odata.streaming=true; charset=utf-8
OData-Version: 4.0

{""@odata.context"":""http://localhost/api/tests/$metadata#Books/$entity"",""Id"":""79874b37-ce46-4f4c-aa74-8e02ce4d8b67"",""Isbn"":""1111111111111"",""Title"":""Batch Test #1"",""IsActive"":true}
";

    private const string BatchResponse2 =
@"Content-Type: application/http
Content-Transfer-Encoding: binary
Content-ID: 2

HTTP/1.1 201 Created
Location: http://localhost/api/tests/Books(c6b67ec7-badc-45c6-98c7-c76b570ce694)
Content-Type: application/json; odata.metadata=minimal; odata.streaming=true; charset=utf-8
OData-Version: 4.0

{""@odata.context"":""http://localhost/api/tests/$metadata#Books/$entity"",""Id"":""c6b67ec7-badc-45c6-98c7-c76b570ce694"",""Isbn"":""2222222222222"",""Title"":""Batch Test #2"",""IsActive"":true}
";

    private const string JsonBatchRequest = @"
        {
            ""requests"": [{
                    ""id"": ""1"",
                    ""method"": ""POST"",
                    ""url"": ""http://localhost/api/tests/Books"",
                    ""headers"": {
                        ""OData-Version"": ""4.0"",
                        ""Content-Type"": ""application/json;odata.metadata=minimal"",
                        ""Accept"": ""application/json;odata.metadata=minimal""
                    },
                    ""body"": {
                        ""@odata.context"":""http://localhost/api/tests/$metadata#Books/$entity"",
                        ""Id"":""79874b37-ce46-4f4c-aa74-8e02ce4d8b67"",
                        ""Isbn"":""1111111111111"",
                        ""Title"":""Batch Test #1"",
                        ""IsActive"":true
                    }
                }, {
                    ""id"": ""2"",
                    ""method"": ""POST"",
                    ""url"": ""http://localhost/api/tests/Books"",
                    ""headers"": {
                        ""OData-Version"": ""4.0"",
                        ""Content-Type"": ""application/json;odata.metadata=minimal"",
                        ""Accept"": ""application/json;odata.metadata=minimal""
                    },
                    ""body"": {
                        ""@odata.context"":""http://localhost/api/tests/$metadata#Books/$entity"",
                        ""Id"":""c6b67ec7-badc-45c6-98c7-c76b570ce694"",
                        ""Isbn"":""2222222222222"",
                        ""Title"":""Batch Test #2"",
                        ""IsActive"":true
                    }
                }
            ]
        }";

    private const string JsonBatchResponse = @"{""responses"":[{""id"":""1"",""status"":201,""headers"":{""location"":""http://localhost/api/tests/Books(79874b37-ce46-4f4c-aa74-8e02ce4d8b67)"",""content-type"":""application/json; odata.metadata=minimal; odata.streaming=true; charset=utf-8"",""odata-version"":""4.0""}, ""body"" :{""@odata.context"":""http://localhost/api/tests/$metadata#Books/$entity"",""Id"":""79874b37-ce46-4f4c-aa74-8e02ce4d8b67"",""Isbn"":""1111111111111"",""Title"":""Batch Test #1"",""IsActive"":true}},{""id"":""2"",""status"":201,""headers"":{""location"":""http://localhost/api/tests/Books(c6b67ec7-badc-45c6-98c7-c76b570ce694)"",""content-type"":""application/json; odata.metadata=minimal; odata.streaming=true; charset=utf-8"",""odata-version"":""4.0""}, ""body"" :{""@odata.context"":""http://localhost/api/tests/$metadata#Books/$entity"",""Id"":""c6b67ec7-badc-45c6-98c7-c76b570ce694"",""Isbn"":""2222222222222"",""Title"":""Batch Test #2"",""IsActive"":true}}]}";


    private const string SelectPlusFunctionBatchRequest = @"
        {
            ""requests"": [{
                    ""id"": ""1"",
                    ""method"": ""GET"",
                    ""url"": ""http://localhost/api/tests/Publishers('Publisher1')"",
                    ""headers"": {
                        ""OData-Version"": ""4.0"",
                        ""Accept"": ""application/json;odata.metadata=minimal""
                    }
                }, {
                    ""id"": ""2"",
                    ""method"": ""GET"",
                    ""url"": ""http://localhost/api/tests/PublishBook(IsActive=true)"",
                    ""headers"": {
                        ""OData-Version"": ""4.0"",
                        ""Accept"": ""application/json;odata.metadata=minimal""
                    }
                }
            ]
        }";
}
