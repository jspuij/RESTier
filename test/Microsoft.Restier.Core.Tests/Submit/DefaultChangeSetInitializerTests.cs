// <copyright file="DefaultChangeSetInitializerTests.cs" company="Microsoft Corporation">
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
    /// Unit tests for the <see cref="DefaultChangeSetInitializer"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DefaultChangeSetInitializerTests : IClassFixture<ServiceProviderFixture>
    {
        private readonly ServiceProviderFixture serviceProviderFixture;
        private DefaultChangeSetInitializer testClass;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultChangeSetInitializerTests"/> class.
        /// </summary>
        /// <param name="serviceProviderFixture">The <see cref="IServiceProvider"/> fixture.</param>
        public DefaultChangeSetInitializerTests(ServiceProviderFixture serviceProviderFixture)
        {
            this.testClass = new DefaultChangeSetInitializer();
            this.serviceProviderFixture = serviceProviderFixture;
        }

        /// <summary>
        /// Can construct an instance of the <see cref="DefaultChangeSetInitializer"/> class.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var instance = new DefaultChangeSetInitializer();
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Can call InitializeAsync.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CanCallInitializeAsync()
        {
            var context = new SubmitContext(new TestApi(this.serviceProviderFixture.ServiceProvider), null);
            var cancellationToken = CancellationToken.None;
            await this.testClass.InitializeAsync(context, cancellationToken);
            context.ChangeSet.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot call InitializeAsync with a null ontext.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CannotCallInitializeAsyncWithNullContext()
        {
            Func<Task> act = () => this.testClass.InitializeAsync(default(SubmitContext), CancellationToken.None);
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