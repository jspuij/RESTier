// <copyright file="ConventionBasedChangeSetItemFilterTests.cs" company="Microsoft Corporation">
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
    /// Unit tests for the <see cref="ConventionBasedChangeSetItemFilter"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ConventionBasedChangeSetItemFilterTests : IClassFixture<ServiceProviderFixture>
    {
        private readonly IServiceProvider serviceProvider;
        private readonly DataModificationItem dataModificationItem;
        private readonly TestTraceListener testTraceListener = new TestTraceListener();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConventionBasedChangeSetItemFilterTests"/> class.
        /// </summary>
        /// <param name="serviceProviderFixture">A fixture for <see cref="IServiceProvider"/>.</param>
        public ConventionBasedChangeSetItemFilterTests(ServiceProviderFixture serviceProviderFixture)
        {
            this.serviceProvider = serviceProviderFixture.ServiceProvider;
            this.dataModificationItem = new DataModificationItem(
                "Test",
                typeof(object),
                typeof(object),
                RestierEntitySetOperation.Insert,
                new Dictionary<string, object>(),
                new Dictionary<string, object>(),
                new Dictionary<string, object>())
            {
                Resource = new object(),
            };
            Trace.Listeners.Add(this.testTraceListener);
        }

        /// <summary>
        /// Checks whether the <see cref="ConventionBasedChangeSetItemFilter"/> can be constructed.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var instance = new ConventionBasedChangeSetItemFilter(typeof(EmptyApi));
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Checks that the constructor cannot be called with a null type.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullTargetType()
        {
            Action act = () => new ConventionBasedChangeSetItemFilter(default(Type));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Check that OnChangeSetItemProcessingAsync can be called.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CanCallOnChangeSetItemProcessingAsync()
        {
            var testClass = new ConventionBasedChangeSetItemFilter(typeof(EmptyApi));
            var context = new SubmitContext(new EmptyApi(this.serviceProvider), new ChangeSet());
            var cancellationToken = CancellationToken.None;
            await testClass.OnChangeSetItemProcessingAsync(context, this.dataModificationItem, cancellationToken);
        }

        /// <summary>
        /// Check that OnChangeSetItemProcessingAsync invokes the OnInsertingObject method according to convention.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task OnChangeSetItemProcessingAsyncInvokesConventionMethod()
        {
            var api = new InsertApi(this.serviceProvider);
            var context = new SubmitContext(api, new ChangeSet());
            var cancellationToken = CancellationToken.None;
            var testClass = new ConventionBasedChangeSetItemFilter(typeof(InsertApi));
            await testClass.OnChangeSetItemProcessingAsync(context, this.dataModificationItem, cancellationToken);
            api.InvocationCount.Should().Be(1);
        }

        /// <summary>
        /// Checks that OnChangeSetItemProcessingAsync throws when the submit context is null.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CannotCallOnChangeSetItemProcessingAsyncWithNullContext()
        {
            var testClass = new ConventionBasedChangeSetItemFilter(typeof(EmptyApi));
            Func<Task> act = () => testClass.OnChangeSetItemProcessingAsync(
                default(SubmitContext),
                this.dataModificationItem,
                CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Checks that OnChangeSetItemProcessingAsync throws when the item is null.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CannotCallOnChangeSetItemProcessingAsyncWithNullItem()
        {
            var testClass = new ConventionBasedChangeSetItemFilter(typeof(EmptyApi));
            Func<Task> act = () => testClass.OnChangeSetItemProcessingAsync(
                new SubmitContext(new EmptyApi(this.serviceProvider), new ChangeSet()),
                default(ChangeSetItem),
                CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Check that OnChangeSetItemProcessedAsync can be called.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CanCallOnChangeSetItemProcessedAsync()
        {
            var testClass = new ConventionBasedChangeSetItemFilter(typeof(EmptyApi));
            var context = new SubmitContext(new EmptyApi(this.serviceProvider), new ChangeSet());
            var cancellationToken = CancellationToken.None;
            await testClass.OnChangeSetItemProcessedAsync(context, this.dataModificationItem, cancellationToken);
        }

        /// <summary>
        /// Check that OnChangeSetItemProcessedAsync invokes the OnInsertedObject method according to convention.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task OnChangeSetItemProcessedAsyncInvokesConventionMethod()
        {
            var api = new InsertApi(this.serviceProvider);
            var context = new SubmitContext(api, new ChangeSet());
            var cancellationToken = CancellationToken.None;
            var testClass = new ConventionBasedChangeSetItemFilter(typeof(InsertApi));
            await testClass.OnChangeSetItemProcessedAsync(context, this.dataModificationItem, cancellationToken);
            api.InvocationCount.Should().Be(1);
        }

        /// <summary>
        /// Check that OnChangeSetItemProcessingAsync does not invoke OnInsertingObject because of an incorrect visibility.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task OnChangeSetItemProcessingAsyncWithPrivateMethod()
        {
            this.testTraceListener.Clear();
            var api = new PrivateMethodApi(this.serviceProvider);
            var testClass = new ConventionBasedChangeSetItemFilter(typeof(PrivateMethodApi));
            var context = new SubmitContext(api, new ChangeSet());
            var cancellationToken = CancellationToken.None;
            await testClass.OnChangeSetItemProcessingAsync(context, this.dataModificationItem, cancellationToken);
            this.testTraceListener.Messages.Should().Contain("inaccessible due to its protection level");
            api.InvocationCount.Should().Be(0);
        }

        /// <summary>
        /// Check that OnChangeSetItemProcessingAsync does not invoke OnInsertingObject because of a wrong return type.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task OnChangeSetItemProcessingWithWrongReturnType()
        {
            this.testTraceListener.Clear();
            var api = new WrongReturnTypeApi(this.serviceProvider);
            var testClass = new ConventionBasedChangeSetItemFilter(typeof(WrongReturnTypeApi));
            var context = new SubmitContext(api, new ChangeSet());
            var cancellationToken = CancellationToken.None;
            await testClass.OnChangeSetItemProcessingAsync(context, this.dataModificationItem, cancellationToken);
            this.testTraceListener.Messages.Should().Contain("does not return");
            api.InvocationCount.Should().Be(0);
        }

        /// <summary>
        /// Check that OnChangeSetItemProcessingAsync does not invoke OnInsertingTest because of a wrong resource name.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task OnChangeSetItemProcessingWithWrongMethod()
        {
            this.testTraceListener.Clear();
            var api = new WrongMethodApi(this.serviceProvider);
            var testClass = new ConventionBasedChangeSetItemFilter(typeof(WrongMethodApi));
            var context = new SubmitContext(api, new ChangeSet());
            var cancellationToken = CancellationToken.None;
            await testClass.OnChangeSetItemProcessingAsync(context, this.dataModificationItem, cancellationToken);
            this.testTraceListener.Messages.Should().Contain("Restier Filter expected'OnInsertingObject' but found 'OnInsertingTest'");
            api.InvocationCount.Should().Be(0);
        }

        /// <summary>
        /// Check that OnChangeSetItemProcessingAsync does not invoke OnInsertingObject because of a wrong api type.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task OnChangeSetItemProcessingWithWrongApiType()
        {
            this.testTraceListener.Clear();
            var api = new PrivateMethodApi(this.serviceProvider);
            var testClass = new ConventionBasedChangeSetItemFilter(typeof(InsertApi));
            var context = new SubmitContext(api, new ChangeSet());
            var cancellationToken = CancellationToken.None;
            await testClass.OnChangeSetItemProcessingAsync(context, this.dataModificationItem, cancellationToken);
            this.testTraceListener.Messages.Should().Contain("is of the incorrect type");
            api.InvocationCount.Should().Be(0);
        }

        /// <summary>
        /// Check that OnChangeSetItemProcessingAsync does not invoke OnInsertingObject because of a wrong number of arguments.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task OnChangeSetItemProcessingWithWrongNumberOfArguments()
        {
            this.testTraceListener.Clear();
            var api = new IncorrectArgumentsApi(this.serviceProvider);
            var testClass = new ConventionBasedChangeSetItemFilter(typeof(IncorrectArgumentsApi));
            var context = new SubmitContext(api, new ChangeSet());
            var cancellationToken = CancellationToken.None;
            await testClass.OnChangeSetItemProcessingAsync(context, this.dataModificationItem, cancellationToken);
            this.testTraceListener.Messages.Should().Contain("incorrect number of arguments");
            api.InvocationCount.Should().Be(0);
        }

        /// <summary>
        /// Checks that OnChangeSetItemProcessedAsync throws when the submit context is null.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CannotCallOnChangeSetItemProcessedAsyncWithNullContext()
        {
            var testClass = new ConventionBasedChangeSetItemFilter(typeof(EmptyApi));
            Func<Task> act = () => testClass.OnChangeSetItemProcessedAsync(
                default(SubmitContext),
                this.dataModificationItem,
                CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Checks that OnChangeSetItemProcessedAsync throws when the item is null.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CannotCallOnChangeSetItemProcessedAsyncWithNullItem()
        {
            var testClass = new ConventionBasedChangeSetItemFilter(typeof(EmptyApi));
            Func<Task> act = () => testClass.OnChangeSetItemProcessedAsync(
                new SubmitContext(new EmptyApi(this.serviceProvider), new ChangeSet()),
                default(ChangeSetItem),
                CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        private class EmptyApi : ApiBase
        {
            public EmptyApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }
        }

        private class InsertApi : ApiBase
        {
            public InsertApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public int InvocationCount { get; private set; }

            protected void OnInsertingObject(object o)
            {
                this.InvocationCount++;
            }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            protected async Task OnInsertedObject(object o)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                this.InvocationCount++;
            }
        }

        private class PrivateMethodApi : ApiBase
        {
            public PrivateMethodApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public int InvocationCount { get; private set; }

            private void OnInsertingObject(object o)
            {
                this.InvocationCount++;
            }
        }

        private class WrongReturnTypeApi : ApiBase
        {
            public WrongReturnTypeApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public int InvocationCount { get; private set; }

            protected internal int OnInsertingObject(object o)
            {
                this.InvocationCount++;
                return 0;
            }
        }

        private class WrongMethodApi : ApiBase
        {
            public WrongMethodApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public int InvocationCount { get; private set; }

            protected internal void OnInsertingTest(object o)
            {
                this.InvocationCount++;
            }
        }

        private class IncorrectArgumentsApi : ApiBase
        {
            public IncorrectArgumentsApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public int InvocationCount { get; private set; }

            protected internal void OnInsertingObject(int arg)
            {
                this.InvocationCount++;
            }
        }
    }
}