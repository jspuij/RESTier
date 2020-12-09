// <copyright file="AuthorizationTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.AspNet.Tests.FeatureTests
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Restier.Core.Query;
    using Microsoft.Restier.EntityFramework;
    using Microsoft.Restier.Tests.Shared;
    using Microsoft.Restier.Tests.Shared.AspNet;
    using Microsoft.Restier.Tests.Shared.AspNet.Extensions;
    using Microsoft.Restier.Tests.Shared.AspNet.Scenarios.Library;
    using Microsoft.Restier.Tests.Shared.Common;
    using Microsoft.Restier.Tests.Shared.EntityFramework.Scenarios.Library;
    using Microsoft.Restier.Tests.Shared.Scenarios.Library;
    using Newtonsoft.Json;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Authorization Tests.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AuthorizationTests
    {
        private readonly ITestOutputHelper output;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationTests"/> class.
        /// </summary>
        /// <param name="output">The helper to output into during the tests.</param>
        public AuthorizationTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Tests if the query pipeline is correctly returning 403 StatusCodes when <see cref="IQueryExpressionAuthorizer.Authorize(QueryExpressionContext)"/> returns false.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task Authorization_FilterReturns403()
        {
            void DiSetup(IServiceCollection services)
            {
                services.AddEF6ProviderServices<LibraryContext>()
                    .AddSingleton<IQueryExpressionAuthorizer, DisallowEverythingAuthorizer>();
            }

            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Books", serviceCollection: DiSetup);
            var content = await response.Content.ReadAsStringAsync();
            this.output.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// Tests that updating an employee should return 400.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task UpdateEmployee_ShouldReturn400()
        {
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter>
                {
                    new JsonTimeSpanConverter(),
                    new JsonTimeOfDayConverter(),
                },
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatString = "yyyy-MM-ddTHH:mm:ssZ",
            };

            var employeeRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Readers?$top=1", acceptHeader: ODataConstants.DefaultAcceptHeader);
            employeeRequest.IsSuccessStatusCode.Should().BeTrue();
            var (employeeList, errorContent) = await employeeRequest.DeserializeResponseAsync<ODataV4List<Employee>>(settings);

            employeeList.Should().NotBeNull();
            employeeList.Items.Should().NotBeEmpty();
            var employee = employeeList.Items.First();

            employee.Should().NotBeNull();

            employee.FullName += " Can't Update";

            var employeeEditResponse = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Put, resource: $"/Readers({employee.Id})", payload: employee, acceptHeader: WebApiConstants.DefaultAcceptHeader, jsonSerializerSettings: settings);
            employeeEditResponse.IsSuccessStatusCode.Should().BeFalse();
            employeeEditResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}