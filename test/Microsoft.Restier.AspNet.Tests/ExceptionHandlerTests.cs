// <copyright file="ExceptionHandlerTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.AspNet.Tests
{
    using System.Data.Entity;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq.Expressions;
    using System.Net;
    using System.Net.Http;
    using System.Security;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Query;
    using Microsoft.Restier.Tests.Shared;
    using Microsoft.Restier.Tests.Shared.AspNet;
    using Microsoft.Restier.Tests.Shared.AspNet.Extensions;
    using Xunit;

    /// <summary>
    /// Unit tests for Exception handling.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ExceptionHandlerTests
    {
        /// <summary>
        /// If the handler throws a security exception, the result should be a 403 foribdden.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ShouldReturn403HandlerThrowsSecurityException()
        {
            void DiSetup(IServiceCollection services)
            {
                services.AddTestStoreApiServices()
                    .AddChainedService<IQueryExpressionSourcer>((sp, next) => new FakeSourcer());
            }

            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi, DbContext>(HttpMethod.Get, resource: "/Products", serviceCollection: DiSetup);
            response.IsSuccessStatusCode.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        private class FakeSourcer : IQueryExpressionSourcer
        {
            public Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded)
            {
                throw new SecurityException();
            }
        }
    }
}
