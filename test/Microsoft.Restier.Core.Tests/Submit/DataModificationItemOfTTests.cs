// <copyright file="DataModificationItemOfTTests.cs" company="Microsoft Corporation">
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
            this.resourceSetName = "Tests";
            this.expectedResourceType = typeof(Test);
            this.actualResourceType = typeof(Test);
            this.action = RestierEntitySetOperation.Update;
            this.resourceKey = new Dictionary<string, object>();
            this.originalValues = new Dictionary<string, object>();
            this.localValues = new Dictionary<string, object>();
            this.testClass = new DataModificationItem<Test>(
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
        /// Can set and get Resource.
        /// </summary>
        [Fact]
        public void CanSetAndGetResource()
        {
            var testValue = new Test { Name = "LoremIpsum", Order = 1 };
            this.testClass.Resource = testValue;
            this.testClass.Resource.Should().Be(testValue);
        }

        private class Test
        {
            public string Name { get; set; }

            public int Order { get; set; }
        }
    }
}