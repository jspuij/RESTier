// <copyright file="ODataControllerFallbackTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.AspNet.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using FluentAssertions;
    using Microsoft.AspNet.OData;
    using Microsoft.AspNet.OData.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.OData.Edm;
    using Microsoft.Restier.AspNet.Model;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Model;
    using Microsoft.Restier.Core.Query;
    using Microsoft.Restier.Core.Submit;
    using Microsoft.Restier.Tests.Shared;
    using Microsoft.Restier.Tests.Shared.AspNet;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Tests fallback to an OData Controller if available.
    /// </summary>
    public class ODataControllerFallbackTests
    {
        private readonly ITestOutputHelper output;

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataControllerFallbackTests"/> class.
        /// </summary>
        /// <param name="output">The helper to output into during the tests.</param>
        public ODataControllerFallbackTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// EntitySet should fallback.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task FallbackApi_EntitySet_ShouldFallBack()
        {
            // Should fallback to PeopleController.
            var response = await RestierTestHelpers.ExecuteTestRequest<FallbackApi, DbContext>(HttpMethod.Get, resource: "/People", serviceCollection: this.AddTestServices);
            this.output.WriteLine(await response.Content.ReadAsStringAsync());
            response.IsSuccessStatusCode.Should().BeTrue();
            ((Person[])((ObjectContent)response.Content).Value).Single().Id.Should().Be(999);
        }

        /// <summary>
        /// Navigation property should fallback.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task FallbackApi_NavigationProperty_ShouldFallBack()
        {
            // Should fallback to PeopleController.
            var response = await RestierTestHelpers.ExecuteTestRequest<FallbackApi, DbContext>(HttpMethod.Get, resource: "/People(1)/Orders", serviceCollection: this.AddTestServices);
            this.output.WriteLine(await response.Content.ReadAsStringAsync());
            response.IsSuccessStatusCode.Should().BeTrue();
            ((Order[])((ObjectContent)response.Content).Value).Single().Id.Should().Be(123);
        }

        /// <summary>
        /// EntitySet should not fallback.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task FallbackApi_EntitySet_ShouldNotFallBack()
        {
            // Should be routed to RestierController.
            var response = await RestierTestHelpers.ExecuteTestRequest<FallbackApi, DbContext>(HttpMethod.Get, resource: "/Orders", serviceCollection: this.AddTestServices);
            this.output.WriteLine(await response.Content.ReadAsStringAsync());
            response.IsSuccessStatusCode.Should().BeTrue();
            (await response.Content.ReadAsStringAsync()).Should().Contain("\"Id\":234");
        }

        /// <summary>
        /// Resource should not fallback.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task FallbackApi_Resource_ShouldNotFallBack()
        {
            // Should be routed to RestierController.
            var response = await RestierTestHelpers.ExecuteTestRequest<FallbackApi, DbContext>(HttpMethod.Get, resource: "/PreservedOrders", serviceCollection: this.AddTestServices);
            this.output.WriteLine(await response.Content.ReadAsStringAsync());
            response.IsSuccessStatusCode.Should().BeTrue();
            (await response.Content.ReadAsStringAsync()).Should().Contain("\"Id\":234");
        }

        private void AddTestServices(IServiceCollection services)
        {
            services.AddChainedService<IModelBuilder>((sp, next) => new StoreModelProducer(FallbackModel.Model))
                .AddChainedService<IModelMapper>((sp, next) => new FallbackModelMapper())
                .AddChainedService<IQueryExpressionSourcer>((sp, next) => new FallbackQueryExpressionSourcer())
                .AddChainedService<IChangeSetInitializer>((sp, next) => new StoreChangeSetInitializer())
                .AddChainedService<ISubmitExecutor>((sp, next) => new DefaultSubmitExecutor());
        }
    }

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1600 // Elements should be documented
    /// <summary>
    /// People controller.
    /// </summary>
    public class PeopleController : ODataController
    {
        /// <summary>
        /// Gets a person.
        /// </summary>
        /// <returns>An action result with the person.</returns>
        public IHttpActionResult Get()
        {
            var people = new[]
            {
                new Person { Id = 999 },
            };

            return this.Ok(people);
        }

        /// <summary>
        /// Gets some orders by key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The orders.</returns>
        public IHttpActionResult GetOrders(int key)
        {
            var orders = new[]
            {
                new Order { Id = 123 },
            };

            return this.Ok(orders);
        }
    }

    internal static class FallbackModel
    {
        static FallbackModel()
        {
            var builder = new ODataConventionModelBuilder
            {
                Namespace = "Microsoft.Restier.AspNet.Tests",
            };
            builder.EntitySet<Order>("Orders");
            builder.EntitySet<Person>("People");
            Model = (EdmModel)builder.GetEdmModel();
        }

        public static EdmModel Model { get; private set; }
    }

    internal class FallbackApi : ApiBase
    {
        public FallbackApi(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        [Resource]
        public IQueryable<Order> PreservedOrders => this.GetQueryableSource<Order>("Orders").Where(o => o.Id > 123);
    }

    internal class Person
    {
        public int Id { get; set; }

        public IEnumerable<Order> Orders { get; set; }
    }

    internal class Order
    {
        public int Id { get; set; }
    }

    internal class FallbackQueryExpressionSourcer : IQueryExpressionSourcer
    {
        public Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded)
        {
            var orders = new[]
            {
                new Order { Id = 234 },
            };

            if (!embedded)
            {
                if (context.VisitedNode.ToString().StartsWith("GetQueryableSource(\"Orders\"", StringComparison.CurrentCulture))
                {
                    return Expression.Constant(orders.AsQueryable());
                }
            }

            return context.VisitedNode;
        }
    }

    internal class FallbackModelMapper : IModelMapper
    {
        public bool TryGetRelevantType(ModelContext context, string name, out Type relevantType)
        {
            relevantType = name == "Person" ? typeof(Person) : typeof(Order);

            return true;
        }

        public bool TryGetRelevantType(ModelContext context, string namespaceName, string name, out Type relevantType) => this.TryGetRelevantType(context, name, out relevantType);
    }
#pragma warning restore SA1600 // Elements should be documented
#pragma warning restore SA1402 // File may only contain a single type
}