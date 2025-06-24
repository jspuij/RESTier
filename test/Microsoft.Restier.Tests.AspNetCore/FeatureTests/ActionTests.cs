// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.Breakdance.AspNetCore;
using Microsoft.AspNetCore.Http;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using System.Diagnostics;
using System.Net;
using Xunit;
using Microsoft.Restier.Tests.Shared.Extensions;
using System.Threading;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests
{

    /// <summary>
    /// A class for testing OData Actions.
    /// </summary>
    public class ActionTests(ITestOutputHelper outputHelper) : RestierTestBase
#if NET6_0_OR_GREATER
        <LibraryApi>
#endif
    {
        /* JHC note: just leaving this here temporarily for reference
        #if EF6
                void addTestServices<TDbContext>(IServiceCollection services) where TDbContext : DbContext => services.AddEF6ProviderServices<TDbContext>();
        #endif

        #if EFCore
                void addTestServices<TDbContext>(IServiceCollection services) where TDbContext : DbContext => services.AddEFCoreProviderServices<TDbContext>();
        #endif
        */
        //[Ignore]
        [Fact]
        public async Task ActionParameters_MissingParameter()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Post, resource: "/CheckoutBook", serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var content = await TraceListener.LogAndReturnMessageContentAsync(response);
            outputHelper.Write(content);
            response.IsSuccessStatusCode.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            content.Should().Contain("ArgumentNullException");
        }

        [Fact]
        public async Task ActionParameters_WrongParameterName()
        {
            var bookPayload = new {
                john = new Book
                {
                    Id = Guid.NewGuid(),
                    Title = "Constantly Frustrated: the Robert McLaws Story",
                }
            };

            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Post, resource: "/CheckoutBook", acceptHeader: WebApiConstants.DefaultAcceptHeader, payload: bookPayload, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var content = await TraceListener.LogAndReturnMessageContentAsync(response);
            outputHelper.Write(content);
            response.IsSuccessStatusCode.Should().BeFalse();

            content.Should().Contain("Model state is not valid");
        }

        [Fact]
        public async Task ActionParameters_HasParameter()
        {
            var bookPayload = new {
                book = new Book
                {
                    Id = Guid.NewGuid(),
                    Title = "Constantly Frustrated: the Robert McLaws Story",
                }
            };

            //var response = await RestierTestHelpers.RouteDebug<LibraryApi>(serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Books", acceptHeader: WebApiConstants.DefaultAcceptHeader, payload: bookPayload, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());

            //var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Post, resource: "/CheckoutBook", acceptHeader: WebApiConstants.DefaultAcceptHeader, payload: bookPayload, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var content = await TraceListener.LogAndReturnMessageContentAsync(response);
            outputHelper.Write(content);
            response.IsSuccessStatusCode.Should().BeTrue();

            content.Should().Contain("Robert McLaws");
            content.Should().Contain("| Submitted");
        }

    }

}