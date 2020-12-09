// <copyright file="BatchTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.AspNet.Tests.FeatureTests
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Restier.Tests.Shared.AspNet;
    using Microsoft.Restier.Tests.Shared.AspNet.Extensions;
    using Microsoft.Restier.Tests.Shared.AspNet.Scenarios.Library;
    using Microsoft.Restier.Tests.Shared.EntityFramework.Scenarios.Library;
    using Microsoft.Restier.Tests.Shared.Scenarios.Library;
    using Simple.OData.Client;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Batch operation tests.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class BatchTests
    {
        private readonly ITestOutputHelper output;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchTests"/> class.
        /// </summary>
        /// <param name="output">The helper to output into during the tests.</param>
        public BatchTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Add multiple entries.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task BatchTests_AddMultipleEntries()
        {
            var config = await RestierTestHelpers.GetTestableRestierConfiguration<LibraryApi, LibraryContext>().ConfigureAwait(false);
            var httpClient = config.GetTestableHttpClient();
            httpClient.BaseAddress = new Uri($"{WebApiConstants.Localhost}{WebApiConstants.RoutePrefix}");

            var odataSettings = new ODataClientSettings(httpClient, new Uri(string.Empty, UriKind.Relative))
            {
                OnTrace = (x, y) => this.output.WriteLine(string.Format(x, y)),

                // RWM: Need a batter way to capture the payload... this event fires before the payload is written to the stream.
                // BeforeRequestAsync = async (x) => {
                //    var ms = new MemoryStream();
                //    if (x.Content != null)
                //    {
                //        await x.Content.CopyToAsync(ms).ConfigureAwait(false);
                //        var streamContent = new StreamContent(ms);
                //        var request = await streamContent.ReadAsStringAsync();
                //        TestContext.WriteLine(request);
                //    }
                // },
                // AfterResponseAsync = async (x) => TestContext.WriteLine(await x.Content.ReadAsStringAsync()),
            };

            var odataBatch = new ODataBatch(odataSettings);
            var odataClient = new ODataClient(odataSettings);

            var publisher = await odataClient.For<Publisher>()
                .Key("Publisher1")
                .FindEntryAsync();

            odataBatch += async c =>
                await c.For<Book>()
                .Set(new { Id = Guid.NewGuid(), Isbn = "1111111111111", Title = "Batch Test #1", Publisher = publisher })
                .InsertEntryAsync();

            odataBatch += async c =>
                await c.For<Book>()
                .Set(new { Id = Guid.NewGuid(), Isbn = "2222222222222", Title = "Batch Test #2", Publisher = publisher })
                .InsertEntryAsync();

            // RWM: This way should also work.
            // var payload = odataBatch.ToString();
            try
            {
                await odataBatch.ExecuteAsync();
            }
            catch (WebRequestException exception)
            {
                this.output.WriteLine(exception.Response);
                throw;
            }

            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Books?$expand=Publisher");
            var content = await response.Content.ReadAsStringAsync();
            this.output.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();

            content.Should().Contain("1111111111111");
            content.Should().Contain("2222222222222");
        }

        /// <summary>
        /// Tests a select plush a function result.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task BatchTests_SelectPlusFunctionResult()
        {
            var config = await RestierTestHelpers.GetTestableRestierConfiguration<LibraryApi, LibraryContext>().ConfigureAwait(false);
            var httpClient = config.GetTestableHttpClient();
            httpClient.BaseAddress = new Uri($"{WebApiConstants.Localhost}{WebApiConstants.RoutePrefix}");

            var odataSettings = new ODataClientSettings(httpClient, new Uri(string.Empty, UriKind.Relative))
            {
                OnTrace = (x, y) => this.output.WriteLine(string.Format(x, y)),

                // RWM: Need a batter way to capture the payload... this event fires before the payload is written to the stream.
                // BeforeRequestAsync = async (x) => {
                //    var ms = new MemoryStream();
                //    if (x.Content != null)
                //    {
                //        await x.Content.CopyToAsync(ms).ConfigureAwait(false);
                //        var streamContent = new StreamContent(ms);
                //        var request = await streamContent.ReadAsStringAsync();
                //        TestContext.WriteLine(request);
                //    }
                // },
                // AfterResponseAsync = async (x) => TestContext.WriteLine(await x.Content.ReadAsStringAsync()),
            };

            var odataBatch = new ODataBatch(odataSettings);
            var odataClient = new ODataClient(odataSettings);

            Publisher publisher = null;
            Book book = null;

            odataBatch += async c =>
                publisher = await odataClient
                    .For<Publisher>()
                    .Key("Publisher1")
                    .FindEntryAsync();

            odataBatch += async c =>
            {
                book = await c
                    .Unbound<Book>()
                    .Function("PublishBook")
                    .Set(new { IsActive = true })
                    .ExecuteAsSingleAsync();
            };

            // RWM: This way should also work.
            // var payload = odataBatch.ToString();
            try
            {
                await odataBatch.ExecuteAsync();
            }
            catch (WebRequestException exception)
            {
                this.output.WriteLine(exception.Response);
                throw;
            }

            publisher.Should().NotBeNull();
            publisher.Addr.Zip.Should().Be("00010");
            book.Should().NotBeNull();
            book.Title.Should().Be("The Cat in the Hat");
        }
    }
}
