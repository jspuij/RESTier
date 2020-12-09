// <copyright file="ChangeSetTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Tests.Submit
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using FluentAssertions;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Submit;
    using Moq;
    using Xunit;

    /// <summary>
    /// Unit tests for the <see cref="ChangeSet"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ChangeSetTests
    {
        private ChangeSet testClass;
        private IEnumerable<ChangeSetItem> entries;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeSetTests"/> class.
        /// </summary>
        public ChangeSetTests()
        {
            this.entries = new[]
            {
                new DataModificationItem<string>(
                    "Tests",
                    typeof(Test),
                    typeof(Test),
                    RestierEntitySetOperation.Insert,
                    new Mock<IReadOnlyDictionary<string, object>>().Object,
                    new Mock<IReadOnlyDictionary<string, object>>().Object,
                    new Mock<IReadOnlyDictionary<string, object>>().Object),
                new DataModificationItem<string>(
                    "People",
                    typeof(Person),
                    typeof(Person),
                    RestierEntitySetOperation.Filter,
                    new Mock<IReadOnlyDictionary<string, object>>().Object,
                    new Mock<IReadOnlyDictionary<string, object>>().Object,
                    new Mock<IReadOnlyDictionary<string, object>>().Object),
                new DataModificationItem<string>(
                    "Orders",
                    typeof(Order),
                    typeof(Order),
                    RestierEntitySetOperation.Update,
                    new Mock<IReadOnlyDictionary<string, object>>().Object,
                    new Mock<IReadOnlyDictionary<string, object>>().Object,
                    new Mock<IReadOnlyDictionary<string, object>>().Object),
            };
            this.testClass = new ChangeSet(this.entries);
        }

        /// <summary>
        /// Can construct.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var instance = new ChangeSet(this.entries);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot constructo with null entries.
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
            this.testClass.Entries.Should().BeEquivalentTo(this.entries);
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