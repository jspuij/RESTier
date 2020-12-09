// <copyright file="ConventionBasedChangeSetItemAuthorizerTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Submit;
    using Microsoft.Restier.Tests.Shared;
    using Xunit;

    /// <summary>
    /// Unit tests for the <see cref="ConventionBasedChangeSetItemAuthorizer"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ConventionBasedChangeSetItemAuthorizerTests : IClassFixture<ServiceProviderFixture>
    {
        private readonly IServiceProvider serviceProvider;
        private readonly DataModificationItem dataModificationItem;
        private readonly TestTraceListener testTraceListener = new TestTraceListener();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConventionBasedChangeSetItemAuthorizerTests"/> class.
        /// </summary>
        /// <param name="serviceProviderFixture">A fixture for <see cref="IServiceProvider"/>.</param>
        public ConventionBasedChangeSetItemAuthorizerTests(ServiceProviderFixture serviceProviderFixture)
        {
            this.serviceProvider = serviceProviderFixture.ServiceProvider;
            this.dataModificationItem = new DataModificationItem(
                "Test",
                typeof(object),
                typeof(object),
                RestierEntitySetOperation.Insert,
                new Dictionary<string, object>(),
                new Dictionary<string, object>(),
                new Dictionary<string, object>());
            Trace.Listeners.Add(this.testTraceListener);
        }

        /// <summary>
        /// Checks whether the <see cref="ConventionBasedChangeSetItemAuthorizer"/> can be constructed.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var instance = new ConventionBasedChangeSetItemAuthorizer(typeof(EmptyApi));
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Checks that the constructor cannot be called with a null type.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullTargetType()
        {
            Action act = () => new ConventionBasedChangeSetItemAuthorizer(default(Type));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Check that AuthorizeAsync can be called and returns true by default.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CanCallAuthorizeAsync()
        {
            var context = new SubmitContext(new EmptyApi(this.serviceProvider), new ChangeSet());
            var cancellationToken = CancellationToken.None;
            var testClass = new ConventionBasedChangeSetItemAuthorizer(typeof(EmptyApi));
            var result = await testClass.AuthorizeAsync(context, this.dataModificationItem, cancellationToken);
            result.Should().BeTrue("AuthorizeAsync should be true by default.");
        }

        /// <summary>
        /// Check that AuthorizeAsync invokes the CanInsertObject method according to convention.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task AuthorizeAsyncInvokesConventionMethod()
        {
            var api = new NoPermissionApi(this.serviceProvider);
            var context = new SubmitContext(api, new ChangeSet());
            var cancellationToken = CancellationToken.None;
            var testClass = new ConventionBasedChangeSetItemAuthorizer(typeof(NoPermissionApi));
            var result = await testClass.AuthorizeAsync(context, this.dataModificationItem, cancellationToken);
            result.Should().BeFalse("AuthorizeAsync should invoke CanInsertObject.");
            api.InvocationCount.Should().Be(1);
        }

        /// <summary>
        /// Check that AuthorizeAsync does not invoke CanInsertObject because of an incorrect visibility.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task AuthorizeAsyncWithPrivateMethod()
        {
            this.testTraceListener.Clear();
            var api = new PrivateMethodApi(this.serviceProvider);
            var testClass = new ConventionBasedChangeSetItemAuthorizer(typeof(PrivateMethodApi));
            var context = new SubmitContext(api, new ChangeSet());
            var cancellationToken = CancellationToken.None;
            var result = await testClass.AuthorizeAsync(context, this.dataModificationItem, cancellationToken);
            result.Should().BeTrue("AuthorizeAsync should return true, because CanInsertObject is private.");
            this.testTraceListener.Messages.Should().Contain("inaccessible due to its protection level");
            api.InvocationCount.Should().Be(0);
        }

        /// <summary>
        /// Check that AuthorizeAsync does not invoke CanInsertObject because of a wrong return type.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task AuthorizeAsyncWithWrongReturnType()
        {
            this.testTraceListener.Clear();
            var api = new WrongReturnTypeApi(this.serviceProvider);
            var testClass = new ConventionBasedChangeSetItemAuthorizer(typeof(WrongReturnTypeApi));
            var context = new SubmitContext(api, new ChangeSet());
            var cancellationToken = CancellationToken.None;
            var result = await testClass.AuthorizeAsync(context, this.dataModificationItem, cancellationToken);
            result.Should().BeTrue("AuthorizeAsync should return true, because CanInsertObject returns an int.");
            this.testTraceListener.Messages.Should().Contain("does not return a boolean value");
            api.InvocationCount.Should().Be(0);
        }

        /// <summary>
        /// Check that AuthorizeAsync does not invoke CanInsertObject because of a wrong api type.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task AuthorizeAsyncWithWrongApiType()
        {
            this.testTraceListener.Clear();
            var api = new WrongReturnTypeApi(this.serviceProvider);
            var testClass = new ConventionBasedChangeSetItemAuthorizer(typeof(NoPermissionApi));
            var context = new SubmitContext(api, new ChangeSet());
            var cancellationToken = CancellationToken.None;
            var result = await testClass.AuthorizeAsync(context, this.dataModificationItem, cancellationToken);
            result.Should().BeTrue("AuthorizeAsync should return true, because the api type is incorrect.");
            this.testTraceListener.Messages.Should().Contain("is of the incorrect type");
            api.InvocationCount.Should().Be(0);
        }

        /// <summary>
        /// Check that AuthorizeAsync does not invoke CanInsertObject because of a wrong number of arguments.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task AuthorizeAsyncWithWrongNumberOfArguments()
        {
            this.testTraceListener.Clear();
            var api = new IncorrectArgumentsApi(this.serviceProvider);
            var testClass = new ConventionBasedChangeSetItemAuthorizer(typeof(IncorrectArgumentsApi));
            var context = new SubmitContext(api, new ChangeSet());
            var cancellationToken = CancellationToken.None;
            var result = await testClass.AuthorizeAsync(context, this.dataModificationItem, cancellationToken);
            result.Should().BeTrue("AuthorizeAsync should return true, because the api type is incorrect.");
            this.testTraceListener.Messages.Should().Contain("incorrect number of arguments");
            api.InvocationCount.Should().Be(0);
        }

        /// <summary>
        /// Checks that AuthorizeAsync throws when the submit context is null.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CannotCallAuthorizeAsyncWithNullContext()
        {
            var testClass = new ConventionBasedChangeSetItemAuthorizer(typeof(EmptyApi));
            Func<Task> act = () => testClass.AuthorizeAsync(
                default(SubmitContext),
                this.dataModificationItem,
                CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Checks that AuthorizeAsync throws when the item. is null.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CannotCallAuthorizeAsyncWithNullItem()
        {
            var testClass = new ConventionBasedChangeSetItemAuthorizer(typeof(EmptyApi));
            Func<Task> act = () => testClass.AuthorizeAsync(new SubmitContext(new EmptyApi(this.serviceProvider), new ChangeSet()), default(ChangeSetItem), CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        private class EmptyApi : ApiBase
        {
            public EmptyApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }
        }

        private class PrivateMethodApi : ApiBase
        {
            public PrivateMethodApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public int InvocationCount { get; private set; }

            private bool CanInsertObject()
            {
                this.InvocationCount++;
                return false;
            }
        }

        private class WrongReturnTypeApi : ApiBase
        {
            public WrongReturnTypeApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public int InvocationCount { get; private set; }

            protected internal int CanInsertObject()
            {
                this.InvocationCount++;
                return 0;
            }
        }

        private class NoPermissionApi : ApiBase
        {
            public NoPermissionApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public int InvocationCount { get; private set; }

            protected internal bool CanInsertObject()
            {
                this.InvocationCount++;
                return false;
            }
        }

        private class IncorrectArgumentsApi : ApiBase
        {
            public IncorrectArgumentsApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public int InvocationCount { get; private set; }

            protected internal bool CanInsertObject(int arg)
            {
                this.InvocationCount++;
                return false;
            }
        }
    }
}