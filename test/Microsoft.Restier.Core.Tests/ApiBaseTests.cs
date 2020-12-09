// <copyright file="ApiBaseTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Submit;
    using Microsoft.Restier.Tests.Shared;
    using Moq;
    using Xunit;

    /// <summary>
    /// Unit tests for the <see cref="ApiBase"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ApiBaseTests : IClassFixture<ServiceProviderFixture>
    {
        private readonly ServiceProviderFixture serviceProviderFixture;
        private TestApiBase testClass;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiBaseTests"/> class.
        /// </summary>
        /// <param name="serviceProviderFixture">The service provider fixture.</param>
        public ApiBaseTests(ServiceProviderFixture serviceProviderFixture)
        {
            this.serviceProviderFixture = serviceProviderFixture;
            this.testClass = new TestApiBase(this.serviceProviderFixture.ServiceProvider);
        }

        /// <summary>
        /// Cannot construct with a null Service provider.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullServiceProvider()
        {
            Action act = () => new TestApiBase(default(IServiceProvider));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Can call SubmitAsync.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CanCallSubmitAsync()
        {
            var changeSet = new ChangeSet();
            changeSet.Entries.Add(
                new DataModificationItem(
                    "Tests",
                    typeof(Test),
                    typeof(Test),
                    RestierEntitySetOperation.Update,
                    new Dictionary<string, object>(),
                    new Dictionary<string, object>(),
                    new Dictionary<string, object>()));
            var cancellationToken = CancellationToken.None;

            bool authCalled = false;

            // check for authorizer invocation.
            this.serviceProviderFixture.ChangeSetItemAuthorizer
                .Setup(x => x.AuthorizeAsync(It.IsAny<SubmitContext>(), It.IsAny<ChangeSetItem>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    authCalled = true;
                    return Task.FromResult(authCalled);
                });

            bool preFilterCalled = false;
            bool postFilterCalled = false;

            // check for filter invocation.
            this.serviceProviderFixture.ChangeSetItemFilter
                .Setup(x => x.OnChangeSetItemProcessingAsync(
                    It.IsAny<SubmitContext>(),
                    It.IsAny<ChangeSetItem>(),
                    It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    preFilterCalled = true;
                    return Task.CompletedTask;
                });
            this.serviceProviderFixture.ChangeSetItemFilter
                .Setup(x => x.OnChangeSetItemProcessedAsync(
                    It.IsAny<SubmitContext>(),
                    It.IsAny<ChangeSetItem>(),
                    It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    postFilterCalled = true;
                    return Task.CompletedTask;
                });

            bool validationCalled = false;

            // check for validator invocation.
            this.serviceProviderFixture.ChangeSetItemValidator
                .Setup(x => x.ValidateChangeSetItemAsync(
                    It.IsAny<SubmitContext>(),
                    It.IsAny<ChangeSetItem>(),
                    It.IsAny<Collection<ChangeSetItemValidationResult>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    validationCalled = true;
                    return Task.FromResult(authCalled);
                });

            var result = await this.testClass.SubmitAsync(changeSet, cancellationToken);
            authCalled.Should().BeTrue("AuthorizeAsync was not called");
            preFilterCalled.Should().BeTrue("OnChangeSetItemProcessingAsync was not called");
            postFilterCalled.Should().BeTrue("OnChangeSetItemProcessedAsync was not called");
            validationCalled.Should().BeTrue("ValidateChangeSetItemAsync was not called");
        }

        /// <summary>
        /// Can call SubmitAsync with unprocessed results. They should be returned immediately.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CanCallSubmitAsyncWithUnprocessedResults()
        {
            var changeSet = new ChangeSet();
            var cancellationToken = CancellationToken.None;
            var submitResult = new SubmitResult(changeSet);

            // setup changeSetInitializer to produce a result immediately.
            this.serviceProviderFixture.ChangeSetInitializer
                .Setup(x => x.InitializeAsync(It.IsAny<SubmitContext>(), It.IsAny<CancellationToken>()))
                .Returns<SubmitContext, CancellationToken>((s, c) =>
            {
                s.Result = submitResult;
                return Task.CompletedTask;
                });
            var result = await this.testClass.SubmitAsync(changeSet, cancellationToken);
            result.Should().Be(submitResult);
        }

        /// <summary>
        /// Cannot call SubmitAsync with a null changeset.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CannotCallSubmitAsyncWithNullChangeSet()
        {
            this.serviceProviderFixture.ChangeSetInitializer.Reset();
            Func<Task> act = () => this.testClass.SubmitAsync(default(ChangeSet), CancellationToken.None);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        /// <summary>
        /// Can call Dispose with no parameters.
        /// </summary>
        [Fact]
        public void CanCallDisposeWithNoParameters()
        {
            this.testClass.Dispose();
            this.testClass.Disposed.Should().BeTrue("ApiBase instance is not disposed.");
        }

        /// <summary>
        /// ServiceProvider is initialized correctly.
        /// </summary>
        [Fact]
        public void ServiceProviderIsInitializedCorrectly()
        {
            this.testClass.ServiceProvider.Should().Be(this.serviceProviderFixture.ServiceProvider);
        }

        private class TestApiBase : ApiBase
        {
            public TestApiBase(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public bool Disposed { get; private set; }

            protected override void Dispose(bool disposing)
            {
                this.Disposed = true;
                base.Dispose(disposing);
            }
        }

        private class Test
        {
            public string Name { get; set; }
        }
    }
}