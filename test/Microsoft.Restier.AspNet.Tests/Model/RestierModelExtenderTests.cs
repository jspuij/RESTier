// <copyright file="RestierModelExtenderTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.AspNet.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.AspNet.OData.Extensions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.OData.Edm;
    using Microsoft.Restier.AspNet.Model;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Model;
    using Microsoft.Restier.Tests.Shared;
    using Microsoft.Restier.Tests.Shared.AspNet;
    using Microsoft.Restier.Tests.Shared.AspNet.Extensions;
    using Microsoft.Restier.Tests.Shared.Extensions;
    using Xunit;

    /// <summary>
    /// Model extender tests.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class RestierModelExtenderTests
    {
        /// <summary>
        /// Tests that an empty api should produce an empty model.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task ApiModelBuilder_ShouldProduceEmptyModelForEmptyApi()
        {
            var model = await RestierTestHelpers.GetTestableModelAsync<TestableEmptyApi, DbContext>(serviceCollection: this.DiEmpty);
            model.SchemaElements.Should().ContainSingle();
            model.EntityContainer.Elements.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that the model builder should produce a correct model for a basic scenario.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task ApiModelBuilder_ShouldProduceCorrectModelForBasicScenario()
        {
            var model = await RestierTestHelpers.GetTestableModelAsync<ApiA, DbContext>(serviceCollection: this.Di);
            model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("ApiConfiguration");
            model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("Invisible");
            model.EntityContainer.FindEntitySet("People").Should().NotBeNull();
            model.EntityContainer.FindSingleton("Me").Should().NotBeNull();
        }

        /// <summary>
        /// Testss that the modelbuilder should produce a correct model for a derived api.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task ApiModelBuilder_ShouldProduceCorrectModelForDerivedApi()
        {
            var model = await RestierTestHelpers.GetTestableModelAsync<ApiB, DbContext>(serviceCollection: this.Di);
            model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("ApiConfiguration");
            model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("Invisible");
            model.EntityContainer.FindEntitySet("Customers").Should().NotBeNull();
            model.EntityContainer.FindEntitySet("People").Should().NotBeNull();
            model.EntityContainer.FindSingleton("Me").Should().NotBeNull();
        }

        /// <summary>
        /// Tests that the modelbuilder should produce the correct model for an overriding property.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task ApiModelBuilder_ShouldProduceCorrectModelForOverridingProperty()
        {
            var model = await RestierTestHelpers.GetTestableModelAsync<ApiC, DbContext>(serviceCollection: this.Di);
            model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("ApiConfiguration");
            model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("Invisible");
            model.EntityContainer.FindEntitySet("People").Should().NotBeNull();
            model.EntityContainer.FindEntitySet("Customers").EntityType().Name.Should().Be("Customer");
            model.EntityContainer.FindSingleton("Me").EntityType().Name.Should().Be("Customer");
        }

        /// <summary>
        /// Tests that the model producer should produce the correct model for ignoring an inherited property.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task ApiModelBuilder_ShouldProduceCorrectModelForIgnoringInheritedProperty()
        {
            var model = await RestierTestHelpers.GetTestableModelAsync<ApiD, DbContext>(serviceCollection: this.Di);
            model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("ApiConfiguration");
            model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("Invisible");
            model.EntityContainer.FindEntitySet("Customers").EntityType().Name.Should().Be("Customer");
            model.EntityContainer.FindSingleton("Me").EntityType().Name.Should().Be("Customer");
        }

        /// <summary>
        /// Tests that the model builder should skip an entity set with an undeclared type.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ApiModelBuilder_ShouldSkipEntitySetWithUndeclaredType()
        {
            var model = await RestierTestHelpers.GetTestableModelAsync<ApiE, DbContext>(serviceCollection: this.Di);
            model.EntityContainer.FindEntitySet("People").EntityType().Name.Should().Be("Person");
            model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("Orders");
        }

        /// <summary>
        /// Tests that the model builder should skip an Existing Entity set.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ApiModelBuilder_ShouldSkipExistingEntitySet()
        {
            var model = await RestierTestHelpers.GetTestableModelAsync<ApiF, DbContext>(serviceCollection: this.Di);
            model.EntityContainer.FindEntitySet("VipCustomers").EntityType().Name.Should().Be("VipCustomer");
        }

        /// <summary>
        /// Tests that the model builder should correctly add bindings for collection navigation propertys.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ApiModelBuilder_ShouldCorrectlyAddBindingsForCollectionNavigationProperty()
        {
            // In this case, only one entity set People has entity type Person.
            // Bindings for collection navigation property Customer.Friends should be added.
            // Bindings for singleton navigation property Customer.BestFriend should be added.
            var model = await RestierTestHelpers.GetTestableModelAsync<ApiC, DbContext>(serviceCollection: this.Di);

            var customersBindings = model.EntityContainer.FindEntitySet("Customers").NavigationPropertyBindings.ToArray();

            var friendsBinding = customersBindings.FirstOrDefault(c => c.NavigationProperty.Name == "Friends");
            friendsBinding.Should().NotBeNull();
            friendsBinding.Target.Name.Should().Be("People");

            var bestFriendBinding = customersBindings.FirstOrDefault(c => c.NavigationProperty.Name == "BestFriend");
            bestFriendBinding.Should().NotBeNull();
            bestFriendBinding.Target.Name.Should().Be("People");

            var meBindings = model.EntityContainer.FindSingleton("Me").NavigationPropertyBindings.ToArray();

            var friendsBinding2 = meBindings.FirstOrDefault(c => c.NavigationProperty.Name == "Friends");
            friendsBinding2.Should().NotBeNull();
            friendsBinding2.Target.Name.Should().Be("People");

            var bestFriendBinding2 = meBindings.FirstOrDefault(c => c.NavigationProperty.Name == "BestFriend");
            bestFriendBinding2.Should().NotBeNull();
            bestFriendBinding2.Target.Name.Should().Be("People");
        }

        /// <summary>
        /// Model builder should correctly add bindings for a singleton navigation property.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ApiModelBuilder_ShouldCorrectlyAddBindingsForSingletonNavigationProperty()
        {
            // In this case, only one singleton Me has entity type Person.
            // Bindings for collection navigation property Customer.Friends should NOT be added.
            // Bindings for singleton navigation property Customer.BestFriend should be added.
            var model = await RestierTestHelpers.GetTestableModelAsync<ApiH, DbContext>(serviceCollection: this.Di);
            var binding = model.EntityContainer.FindEntitySet("Customers").NavigationPropertyBindings.Single();
            binding.NavigationProperty.Name.Should().Be("BestFriend");
            binding.Target.Name.Should().Be("Me");
            binding = model.EntityContainer.FindSingleton("Me2").NavigationPropertyBindings.Single();
            binding.NavigationProperty.Name.Should().Be("BestFriend");
            binding.Target.Name.Should().Be("Me");
        }

        /// <summary>
        /// Tests that the modelbuilder should not add ambiguous navigation property bindings.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ApiModelBuilder_ShouldNotAddAmbiguousNavigationPropertyBindings()
        {
            // In this case, two entity sets Employees and People have entity type Person.
            // Bindings for collection navigation property Customer.Friends should NOT be added.
            // Bindings for singleton navigation property Customer.BestFriend should NOT be added.
            var model = await RestierTestHelpers.GetTestableModelAsync<ApiG, DbContext>(serviceCollection: this.Di);
            model.EntityContainer.FindEntitySet("Customers").NavigationPropertyBindings.Should().BeEmpty();
            model.EntityContainer.FindSingleton("Me").NavigationPropertyBindings.Should().BeEmpty();
        }

        private void Di(IServiceCollection services)
        {
            this.DiEmpty(services);
            services.AddChainedService<IModelBuilder>((sp, next) => new TestModelBuilder());
        }

        private void DiEmpty(IServiceCollection services)
        {
            services.AddTestDefaultServices();
        }

        private class TestModelBuilder : IModelBuilder
        {
            public Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
            {
                var model = new EdmModel();
                var ns = typeof(Person).Namespace;
                var personType = new EdmEntityType(ns, "Person");
                personType.AddKeys(personType.AddStructuralProperty("PersonId", EdmPrimitiveTypeKind.Int32));
                model.AddElement(personType);
                var customerType = new EdmEntityType(ns, "Customer");
                customerType.AddKeys(customerType.AddStructuralProperty("CustomerId", EdmPrimitiveTypeKind.Int32));
                customerType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo
                {
                    Name = "Friends",
                    Target = personType,
                    TargetMultiplicity = EdmMultiplicity.Many,
                });
                customerType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo
                {
                    Name = "BestFriend",
                    Target = personType,
                    TargetMultiplicity = EdmMultiplicity.One,
                });
                model.AddElement(customerType);
                var vipCustomerType = new EdmEntityType(ns, "VipCustomer", customerType);
                model.AddElement(vipCustomerType);
                var container = new EdmEntityContainer(ns, "DefaultContainer");
                container.AddEntitySet("VipCustomers", vipCustomerType);
                model.AddElement(container);
                return Task.FromResult<IEdmModel>(model);
            }
        }
    }

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1600 // Elements should be documented
    internal class Person
    {
        public int PersonId { get; set; }
    }

    internal class ApiA : TestableEmptyApi
    {
        public ApiA(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        [Resource]
        public IQueryable<Person> People { get; set; }

        [Resource]
        public Person Me { get; set; }

        public IQueryable<Person> Invisible { get; set; }
    }

    internal class ApiB : ApiA
    {
        public ApiB(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        [Resource]
        public IQueryable<Person> Customers { get; set; }
    }

    internal class Customer
    {
        public int CustomerId { get; set; }

        public ICollection<Person> Friends { get; set; }

        public Person BestFriend { get; set; }
    }

    internal class VipCustomer : Customer
    {
    }

    internal class ApiC : ApiB
    {
        public ApiC(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        [Resource]
        public new IQueryable<Customer> Customers { get; set; }

        [Resource]
        public new Customer Me { get; set; }
    }

    internal class ApiD : ApiC
    {
        public ApiD(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }
    }

    internal class Order
    {
        public int OrderId { get; set; }
    }

    internal class ApiE : TestableEmptyApi
    {
        public ApiE(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        [Resource]
        public IQueryable<Person> People { get; set; }

        [Resource]
        public IQueryable<Order> Orders { get; set; }
    }

    internal class ApiF : TestableEmptyApi
    {
        public ApiF(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public IQueryable<Customer> VipCustomers { get; set; }
    }

    internal class ApiG : ApiC
    {
        public ApiG(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        [Resource]
        public IQueryable<Person> Employees { get; set; }
    }

    internal class ApiH : TestableEmptyApi
    {
        public ApiH(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        [Resource]
        public Person Me { get; set; }

        [Resource]
        public IQueryable<Customer> Customers { get; set; }

        [Resource]
        public Customer Me2 { get; set; }
    }
#pragma warning restore SA1600 // Elements should be documented
#pragma warning restore SA1402 // File may only contain a single type
}