// <copyright file="SubmitContextTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Tests.Submit
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using FluentAssertions;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Submit;
    using Microsoft.Restier.Tests.Shared;
    using Moq;
    using Xunit;

    /// <summary>
    /// Unit tests for the <see cref="SubmitContext"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class SubmitContextTests : IClassFixture<ServiceProviderFixture>
    {
        private readonly ServiceProviderFixture serviceProviderFixture;
        private SubmitContext testClass;
        private ApiBase api;
        private ChangeSet changeSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubmitContextTests"/> class.
        /// </summary>
        /// <param name="serviceProviderFixture">The service provider fixture.</param>
        public SubmitContextTests(ServiceProviderFixture serviceProviderFixture)
        {
            this.serviceProviderFixture = serviceProviderFixture;
            this.api = new TestApi(this.serviceProviderFixture.ServiceProvider);
            this.changeSet = new ChangeSet();
            this.testClass = new SubmitContext(this.api, this.changeSet);
        }

        /// <summary>
        /// Can construct an instance fo the <see cref="SubmitContext"/> class.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var instance = new SubmitContext(this.api, this.changeSet);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot constructo with a null Api.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullApi()
        {
            Action act = () => new SubmitContext(default(ApiBase), new ChangeSet());
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Changeset is initialized correctly.
        /// </summary>
        [Fact]
        public void ChangeSetIsInitializedCorrectly()
        {
            this.testClass.ChangeSet.Should().Be(this.changeSet);
        }

        /// <summary>
        /// Can set and get the ChangeSet.
        /// </summary>
        [Fact]
        public void CanSetAndGetChangeSet()
        {
            var testValue = new ChangeSet();
            this.testClass.ChangeSet = testValue;
            this.testClass.ChangeSet.Should().Be(testValue);
        }

        /// <summary>
        /// Can set and get the ChangeSet.
        /// </summary>
        [Fact]
        public void CannotSetAndGetChangeSetWithResult()
        {
            var testValue = new ChangeSet();
            this.testClass.ChangeSet = testValue;
            this.testClass.Result = new SubmitResult(this.testClass.ChangeSet);
            Action act = () => this.testClass.ChangeSet = new ChangeSet();
            act.Should().Throw<InvalidOperationException>();
        }

        /// <summary>
        /// Can set and get result.
        /// </summary>
        [Fact]
        public void CanSetAndGetResult()
        {
            var testValue = new SubmitResult(new Exception());
            this.testClass.Result = testValue;
            this.testClass.Result.Should().Be(testValue);
        }

        private class TestApi : ApiBase
        {
            public TestApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }
        }
    }
}