// <copyright file="DataModificationItemTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Tests.Submit
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using FluentAssertions;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Submit;
    using Moq;
    using Xunit;

    /// <summary>
    /// Unit tests for the <see cref="DataModificationItem"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DataModificationItemTests
    {
        private DataModificationItem testClass;
        private string resourceSetName;
        private Type expectedResourceType;
        private Type actualResourceType;
        private RestierEntitySetOperation action;
        private Dictionary<string, object> resourceKey;
        private Dictionary<string, object> originalValues;
        private Dictionary<string, object> localValues;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataModificationItemTests"/> class.
        /// </summary>
        public DataModificationItemTests()
        {
            this.resourceSetName = "Tests";
            this.expectedResourceType = typeof(Test);
            this.actualResourceType = typeof(Test);
            this.action = RestierEntitySetOperation.Update;
            this.resourceKey = new Dictionary<string, object>();
            this.originalValues = new Dictionary<string, object>();
            this.localValues = new Dictionary<string, object>();
            this.testClass = new DataModificationItem(
                this.resourceSetName,
                this.expectedResourceType,
                this.actualResourceType,
                this.action,
                this.resourceKey,
                this.originalValues,
                this.localValues);
        }

        /// <summary>
        /// Can construct the <see cref="DataModificationItem"/> instance.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var instance = new DataModificationItem(
                this.resourceSetName,
                this.expectedResourceType,
                this.actualResourceType,
                this.action,
                this.resourceKey,
                this.originalValues,
                this.localValues);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot construct with null expected resource type.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullExpectedResourceType()
        {
            Action act = () => new DataModificationItem(
                this.resourceSetName,
                default(Type),
                typeof(Test),
                this.action,
                this.resourceKey,
                this.originalValues,
                this.localValues);
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call ApplyTo with a null query.
        /// </summary>
        [Fact]
        public void CannotCallApplyToWithNullQuery()
        {
            Action act = () => this.testClass.ApplyTo(default(IQueryable));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call ApplyTo with an insert operation.
        /// </summary>
        [Fact]
        public void CannotCallApplyToWithInsertOperation()
        {
            var queryable = new List<Test>()
            {
                new Test() { Name = "The" },
                new Test() { Name = "Quick" },
                new Test() { Name = "Brown" },
                new Test() { Name = "Fox" },
            }.AsQueryable();

            this.testClass.EntitySetOperation = RestierEntitySetOperation.Insert;
            Action act = () => this.testClass.ApplyTo(queryable);
            act.Should().Throw<InvalidOperationException>();
        }

        /// <summary>
        /// Cannot call ApplyTo with an Empty set of resource keys.
        /// </summary>
        [Fact]
        public void CannotCallApplyToWithEmptyResourceKey()
        {
            var queryable = new List<Test>()
            {
                new Test() { Name = "The" },
                new Test() { Name = "Quick" },
                new Test() { Name = "Brown" },
                new Test() { Name = "Fox" },
            }.AsQueryable();

            Action act = () => this.testClass.ApplyTo(queryable);
            act.Should().Throw<InvalidOperationException>();
        }

        /// <summary>
        /// Can call apply to.
        /// </summary>
        [Fact]
        public void CanCallApplyTo()
        {
            var queryable = new List<Test>()
            {
                new Test() { Name = "The" },
                new Test() { Name = "Quick" },
                new Test() { Name = "Brown" },
                new Test() { Name = "Fox" },
            }.AsQueryable();

            this.resourceKey.Add("Name", "Quick");

            var result = this.testClass.ApplyTo(queryable);
            result.OfType<Test>().Should().HaveCount(1);
        }

        /// <summary>
        /// Can call apply to with multiple keys.
        /// </summary>
        [Fact]
        public void CanCallApplyToWithMultipleKeys()
        {
            var queryable = new List<Test>()
            {
                new Test() { Name = "The", Order = 1 },
                new Test() { Name = "Quick", Order = 2 },
                new Test() { Name = "Brown", Order = 3 },
                new Test() { Name = "Fox", Order = 4 },
            }.AsQueryable();

            this.resourceKey.Add("Name", "Quick");
            this.resourceKey.Add("Order", 2);

            var result = this.testClass.ApplyTo(queryable);
            result.OfType<Test>().Should().HaveCount(1);
        }

        /// <summary>
        /// Can call ValidateEtag.
        /// </summary>
        [Fact]
        public void CanCallValidateEtag()
        {
            var queryable = new List<Test>()
            {
                new Test() { Name = "Quick", Order = 2 },
            }.AsQueryable();

            this.resourceKey.Add("Name", "Quick");
            this.originalValues.Add("Order", 1);

            Action act = () => this.testClass.ValidateEtag(queryable);
            act.Should().Throw<StatusCodeException>();
        }

        /// <summary>
        /// Can call ValidateEtag with match..
        /// </summary>
        [Fact]
        public void CanCallValidateEtagWithMatch()
        {
            var queryable = new List<Test>()
            {
                new Test() { Name = "Quick", Order = 2 },
            }.AsQueryable();

            this.resourceKey.Add("Name", "Quick");
            this.originalValues.Add("Order", 2);

            this.testClass.ValidateEtag(queryable).Should().Be(queryable.Single());
        }

        /// <summary>
        /// Can call ValidateEtag with match..
        /// </summary>
        [Fact]
        public void CanCallValidateEtagWithIfNoneMatch()
        {
            var queryable = new List<Test>()
            {
                new Test() { Name = "Quick", Order = 2 },
            }.AsQueryable();

            this.resourceKey.Add("Name", "Quick");
            this.originalValues.Add("Order", 1);
            this.originalValues.Add("@IfNoneMatchKey", null);

            this.testClass.ValidateEtag(queryable).Should().Be(queryable.Single());
        }

        /// <summary>
        /// Cannot call ValidateEtag with a null query argument.
        /// </summary>
        [Fact]
        public void CannotCallValidateEtagWithNullQuery()
        {
            Action act = () => this.testClass.ValidateEtag(default(IQueryable));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Checks that the ResourceSetName is initialized correctly.
        /// </summary>
        [Fact]
        public void ResourceSetNameIsInitializedCorrectly()
        {
            this.testClass.ResourceSetName.Should().Be(this.resourceSetName);
        }

        /// <summary>
        /// Checks that the expected resource type is initialized correctly.
        /// </summary>
        [Fact]
        public void ExpectedResourceTypeIsInitializedCorrectly()
        {
            this.testClass.ExpectedResourceType.Should().Be(this.expectedResourceType);
        }

        /// <summary>
        /// Actual resource type is initialized correctly.
        /// </summary>
        [Fact]
        public void ActualResourceTypeIsInitializedCorrectly()
        {
            this.testClass.ActualResourceType.Should().Be(this.actualResourceType);
        }

        /// <summary>
        /// Resource key is initialized correctly.
        /// </summary>
        [Fact]
        public void ResourceKeyIsInitializedCorrectly()
        {
            this.testClass.ResourceKey.Should().BeEquivalentTo(this.resourceKey);
        }

        /// <summary>
        /// Can set and get EntitySetOperation.
        /// </summary>
        [Fact]
        public void CanSetAndGetEntitySetOperation()
        {
            var testValue = RestierEntitySetOperation.Filter;
            this.testClass.EntitySetOperation = testValue;
            this.testClass.EntitySetOperation.Should().Be(testValue);
        }

        /// <summary>
        /// Can set and get IsFullReplaceUpdateRequest.
        /// </summary>
        [Fact]
        public void CanSetAndGetIsFullReplaceUpdateRequest()
        {
            var testValue = true;
            this.testClass.IsFullReplaceUpdateRequest = testValue;
            this.testClass.IsFullReplaceUpdateRequest.Should().Be(testValue);
        }

        /// <summary>
        /// Can set and get Resource.
        /// </summary>
        [Fact]
        public void CanSetAndGetResource()
        {
            var testValue = new object();
            this.testClass.Resource = testValue;
            this.testClass.Resource.Should().Be(testValue);
        }

        /// <summary>
        /// OriginalValues is initialized correctly.
        /// </summary>
        [Fact]
        public void OriginalValuesIsInitializedCorrectly()
        {
            this.testClass.OriginalValues.Should().BeEquivalentTo(this.originalValues);
        }

        /// <summary>
        /// LocalValues is initialized correctly.
        /// </summary>
        [Fact]
        public void LocalValuesIsInitializedCorrectly()
        {
            this.testClass.LocalValues.Should().BeEquivalentTo(this.localValues);
        }

        private class Test
        {
            public string Name { get; set; }

            public int Order { get; set; }
        }
    }
}