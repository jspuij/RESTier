// <copyright file="DefaultSubmitExecutorTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Tests.Submit
{
    using System;
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
    /// Unit tests for the <see cref="DefaultSubmitExecutor"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DefaultSubmitExecutorTests : IClassFixture<ServiceProviderFixture>
    {
        private readonly ServiceProviderFixture serviceProviderFixture;
        private DefaultSubmitExecutor testClass;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultSubmitExecutorTests"/> class.
        /// </summary>
        /// <param name="serviceProviderFixture">Service provider fixture.</param>
        public DefaultSubmitExecutorTests(ServiceProviderFixture serviceProviderFixture)
        {
            this.testClass = new DefaultSubmitExecutor();
            this.serviceProviderFixture = serviceProviderFixture;
        }

        /// <summary>
        /// Can construct.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var instance = new DefaultSubmitExecutor();
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Can call ExecuteSubmitAsync.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CanCallExecuteSubmitAsync()
        {
            var context = new SubmitContext(new TestApi(this.serviceProviderFixture.ServiceProvider), new ChangeSet());
            var cancellationToken = CancellationToken.None;
            var result = await this.testClass.ExecuteSubmitAsync(context, cancellationToken);
            result.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot call ExecuteSubmitAsync with a null context.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CannotCallExecuteSubmitAsyncWithNullContext()
        {
            Func<Task> act = () => this.testClass.ExecuteSubmitAsync(default(SubmitContext), CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
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