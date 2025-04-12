// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Microsoft.Restier.Tests.Core.Submit
{
    /// <summary>
    /// Unit tests for the <see cref="DataModificationItem{T}"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DataModificationItemOfTTests
    {
        private DataModificationItem<Test> testClass;
        private string resourceSetName;
        private Type expectedResourceType;
        private Type actualResourceType;
        private RestierEntitySetOperation action;
        private Dictionary<string, object> resourceKey;
        private Dictionary<string, object> originalValues;
        private Dictionary<string, object> localValues;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataModificationItemOfTTests"/> class.
        /// </summary>
        public DataModificationItemOfTTests()
        {
            resourceSetName = "Tests";
            expectedResourceType = typeof(Test);
            actualResourceType = typeof(Test);
            action = RestierEntitySetOperation.Update;
            resourceKey = new Dictionary<string, object>();
            originalValues = new Dictionary<string, object>();
            localValues = new Dictionary<string, object>();
            testClass = new DataModificationItem<Test>(
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
        /// Can set and get Resource.
        /// </summary>
        [Fact]
        public void CanSetAndGetResource()
        {
            var testValue = new Test { Name = "LoremIpsum", Order = 1 };
            testClass.Resource = testValue;
            testClass.Resource.Should().Be(testValue);
        }

        private class Test
        {
            public string Name { get; set; }

            public int Order { get; set; }
        }
        /// <summary>
        /// Unit tests for the <see cref="ChangeSet"/> class.
        /// </summary>
        [ExcludeFromCodeCoverage]
        public class ChangeSetTests
        {
            private readonly ChangeSet testClass;
            private readonly IEnumerable<ChangeSetItem> entries;

            /// <summary>
            /// Initializes a new instance of the <see cref="ChangeSetTests"/> class.
            /// </summary>
            public ChangeSetTests()
            {
                entries = new[]
                {
                    new DataModificationItem<string>(
                        "Tests",
                        typeof(Test),
                        typeof(Test),
                        RestierEntitySetOperation.Insert,
                        Substitute.For<IReadOnlyDictionary<string, object>>(),
                        Substitute.For<IReadOnlyDictionary<string, object>>(),
                        Substitute.For<IReadOnlyDictionary<string, object>>()),
                    new DataModificationItem<string>(
                        "People",
                        typeof(Person),
                        typeof(Person),
                        RestierEntitySetOperation.Filter,
                        Substitute.For<IReadOnlyDictionary<string, object>>(),
                        Substitute.For<IReadOnlyDictionary<string, object>>(),
                        Substitute.For<IReadOnlyDictionary<string, object>>()),
                    new DataModificationItem<string>(
                        "Orders",
                        typeof(Order),
                        typeof(Order),
                        RestierEntitySetOperation.Update,
                        Substitute.For<IReadOnlyDictionary<string, object>>(),
                        Substitute.For<IReadOnlyDictionary<string, object>>(),
                        Substitute.For<IReadOnlyDictionary<string, object>>()),
                };
                testClass = new ChangeSet(entries);
            }

            /// <summary>
            /// Can construct.
            /// </summary>
            [Fact]
            public void CanConstruct()
            {
                var instance = new ChangeSet(entries);
                instance.Should().NotBeNull();
            }

            /// <summary>
            /// Cannot construct with null entries.
            /// </summary>
            [Fact]
            public void CanConstructWithNullEntries()
            {
                var instance = new ChangeSet();
                instance.Should().NotBeNull();
                instance.Entries.Should().NotBeNull();
            }

            /// <summary>
            /// Entries is initialized correctly.
            /// </summary>
            [Fact]
            public void EntriesIsInitializedCorrectly()
            {
                testClass.Entries.Should().BeEquivalentTo(entries);
            }

            private class Test
            {
            }

            private class Person
            {
            }

            private class Order
            {
            }
        }
    }
}