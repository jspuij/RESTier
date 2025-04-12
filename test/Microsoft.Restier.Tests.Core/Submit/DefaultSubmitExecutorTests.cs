// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using NSubstitute;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.Core.Submit
{

    /// <summary>
    /// Unit tests for the <see cref="DefaultSubmitExecutor"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DefaultSubmitExecutorTests
    {
        private readonly IQueryHandler queryHandler;
        private readonly IEdmModel model;
        private readonly ISubmitHandler submitHandler;
        private DefaultSubmitExecutor testClass;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultSubmitExecutorTests"/> class.
        /// </summary>
        public DefaultSubmitExecutorTests()
        {
            testClass = new DefaultSubmitExecutor();
            queryHandler = Substitute.For<IQueryHandler>();
            model = Substitute.For<IEdmModel>();
            submitHandler = Substitute.For<ISubmitHandler>();
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
            var context = new SubmitContext(new TestApi(model, queryHandler, submitHandler), new ChangeSet());
            var cancellationToken = CancellationToken.None;
            var result = await testClass.ExecuteSubmitAsync(context, cancellationToken);
            result.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot call ExecuteSubmitAsync with a null context.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CannotCallExecuteSubmitAsyncWithNullContext()
        {
            Func<Task> act = () => testClass.ExecuteSubmitAsync(default(SubmitContext), CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        private class TestApi : ApiBase
        {
            public TestApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }
        }
    }
}