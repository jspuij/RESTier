// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Microsoft.Restier.Tests.Core.Submit
{
    /// <summary>
    /// Unit tests for the <see cref="DataModificationItem"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DataModificationItemTests
    {
        private readonly DataModificationItem testClass;
        private readonly string resourceSetName;
        private readonly Type expectedResourceType;
        private readonly Type actualResourceType;
        private readonly RestierEntitySetOperation action;
        private readonly Dictionary<string, object> resourceKey;
        private readonly Dictionary<string, object> originalValues;
        private readonly Dictionary<string, object> localValues;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataModificationItemTests"/> class.
        /// </summary>
        public DataModificationItemTests()
        {
            resourceSetName = "Tests";
            expectedResourceType = typeof(Test);
            actualResourceType = typeof(Test);
            action = RestierEntitySetOperation.Update;
            resourceKey = new Dictionary<string, object>();
            originalValues = new Dictionary<string, object>();
            localValues = new Dictionary<string, object>();
            testClass = new DataModificationItem(
                resourceSetName,
                expectedResourceType,
                actualResourceType,
                action,
                resourceKey,
                originalValues,
                localValues);
        }

        /// <summary>
        /// Can construct the <see cref="DataModificationItem"/> instance.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var instance = new DataModificationItem(
                resourceSetName,
                expectedResourceType,
                actualResourceType,
                action,
                resourceKey,
                originalValues,
                localValues);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot construct with null expected resource type.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullExpectedResourceType()
        {
            Action act = () => new DataModificationItem(
                resourceSetName,
                default(Type),
                typeof(Test),
                action,
                resourceKey,
                originalValues,
                localValues);
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call ApplyTo with a null query.
        /// </summary>
        [Fact]
        public void CannotCallApplyToWithNullQuery()
        {
            Action act = () => testClass.ApplyTo(default(IQueryable));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call ApplyTo with an insert operation.
        /// </summary>
        [Fact]
        public void CannotCallApplyToWithInsertOperation()
        {
            var queryable = new List<Test>
                {
                    new Test { Name = "The" },
                    new Test { Name = "Quick" },
                    new Test { Name = "Brown" },
                    new Test { Name = "Fox" },
                }.AsQueryable();

            testClass.EntitySetOperation = RestierEntitySetOperation.Insert;
            Action act = () => testClass.ApplyTo(queryable);
            act.Should().Throw<InvalidOperationException>();
        }

        /// <summary>
        /// Cannot call ApplyTo with an Empty set of resource keys.
        /// </summary>
        [Fact]
        public void CannotCallApplyToWithEmptyResourceKey()
        {
            var queryable = new List<Test>
                {
                    new Test { Name = "The" },
                    new Test { Name = "Quick" },
                    new Test { Name = "Brown" },
                    new Test { Name = "Fox" },
                }.AsQueryable();

            Action act = () => testClass.ApplyTo(queryable);
            act.Should().Throw<InvalidOperationException>();
        }

        /// <summary>
        /// Can call apply to.
        /// </summary>
        [Fact]
        public void CanCallApplyTo()
        {
            var queryable = new List<Test>
                {
                    new Test { Name = "The" },
                    new Test { Name = "Quick" },
                    new Test { Name = "Brown" },
                    new Test { Name = "Fox" },
                }.AsQueryable();

            resourceKey.Add("Name", "Quick");

            var result = testClass.ApplyTo(queryable);
            result.OfType<Test>().Should().HaveCount(1);
        }

        /// <summary>
        /// Can call apply to with multiple keys.
        /// </summary>
        [Fact]
        public void CanCallApplyToWithMultipleKeys()
        {
            var queryable = new List<Test>
                {
                    new Test { Name = "The", Order = 1 },
                    new Test { Name = "Quick", Order = 2 },
                    new Test { Name = "Brown", Order = 3 },
                    new Test { Name = "Fox", Order = 4 },
                }.AsQueryable();

            resourceKey.Add("Name", "Quick");
            resourceKey.Add("Order", 2);

            var result = testClass.ApplyTo(queryable);
            result.OfType<Test>().Should().HaveCount(1);
        }

        /// <summary>
        /// Can call ValidateEtag.
        /// </summary>
        [Fact]
        public void CanCallValidateEtag()
        {
            var queryable = new List<Test>
                {
                    new Test { Name = "Quick", Order = 2 },
                }.AsQueryable();

            resourceKey.Add("Name", "Quick");
            originalValues.Add("Order", 1);

            Action act = () => testClass.ValidateEtag(queryable);
            act.Should().Throw<StatusCodeException>();
        }

        /// <summary>
        /// Can call ValidateEtag with match.
        /// </summary>
        [Fact]
        public void CanCallValidateEtagWithMatch()
        {
            var queryable = new List<Test>
                {
                    new Test { Name = "Quick", Order = 2 },
                }.AsQueryable();

            resourceKey.Add("Name", "Quick");
            originalValues.Add("Order", 2);

            testClass.ValidateEtag(queryable).Should().Be(queryable.Single());
        }

        /// <summary>
        /// Can call ValidateEtag with IfNoneMatch.
        /// </summary>
        [Fact]
        public void CanCallValidateEtagWithIfNoneMatch()
        {
            var queryable = new List<Test>
                {
                    new Test { Name = "Quick", Order = 2 },
                }.AsQueryable();

            resourceKey.Add("Name", "Quick");
            originalValues.Add("Order", 1);
            originalValues.Add("@IfNoneMatchKey", null);

            testClass.ValidateEtag(queryable).Should().Be(queryable.Single());
        }

        /// <summary>
        /// Cannot call ValidateEtag with a null query argument.
        /// </summary>
        [Fact]
        public void CannotCallValidateEtagWithNullQuery()
        {
            Action act = () => testClass.ValidateEtag(default(IQueryable));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Checks that the ResourceSetName is initialized correctly.
        /// </summary>
        [Fact]
        public void ResourceSetNameIsInitializedCorrectly()
        {
            testClass.ResourceSetName.Should().Be(resourceSetName);
        }

        /// <summary>
        /// Checks that the expected resource type is initialized correctly.
        /// </summary>
        [Fact]
        public void ExpectedResourceTypeIsInitializedCorrectly()
        {
            testClass.ExpectedResourceType.Should().Be(expectedResourceType);
        }

        /// <summary>
        /// Actual resource type is initialized correctly.
        /// </summary>
        [Fact]
        public void ActualResourceTypeIsInitializedCorrectly()
        {
            testClass.ActualResourceType.Should().Be(actualResourceType);
        }

        /// <summary>
        /// Resource key is initialized correctly.
        /// </summary>
        [Fact]
        public void ResourceKeyIsInitializedCorrectly()
        {
            testClass.ResourceKey.Should().BeEquivalentTo(resourceKey);
        }

        /// <summary>
        /// Can set and get EntitySetOperation.
        /// </summary>
        [Fact]
        public void CanSetAndGetEntitySetOperation()
        {
            var testValue = RestierEntitySetOperation.Filter;
            testClass.EntitySetOperation = testValue;
            testClass.EntitySetOperation.Should().Be(testValue);
        }

        /// <summary>
        /// Can set and get IsFullReplaceUpdateRequest.
        /// </summary>
        [Fact]
        public void CanSetAndGetIsFullReplaceUpdateRequest()
        {
            var testValue = true;
            testClass.IsFullReplaceUpdateRequest = testValue;
            testClass.IsFullReplaceUpdateRequest.Should().Be(testValue);
        }

        /// <summary>
        /// Can set and get Resource.
        /// </summary>
        [Fact]
        public void CanSetAndGetResource()
        {
            var testValue = new object();
            testClass.Resource = testValue;
            testClass.Resource.Should().Be(testValue);
        }

        /// <summary>
        /// OriginalValues is initialized correctly.
        /// </summary>
        [Fact]
        public void OriginalValuesIsInitializedCorrectly()
        {
            testClass.OriginalValues.Should().BeEquivalentTo(originalValues);
        }

        /// <summary>
        /// LocalValues is initialized correctly.
        /// </summary>
        [Fact]
        public void LocalValuesIsInitializedCorrectly()
        {
            testClass.LocalValues.Should().BeEquivalentTo(localValues);
        }

        private class Test
        {
            public string Name { get; set; }

            public int Order { get; set; }
        }
    }
}