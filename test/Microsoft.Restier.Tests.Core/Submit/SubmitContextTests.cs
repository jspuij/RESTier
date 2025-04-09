// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Tests.Core.Submit
{
    using FluentAssertions;
    using Microsoft.OData.Edm;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Query;
    using Microsoft.Restier.Core.Submit;
    using NSubstitute;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Xunit;

    /// <summary>
    /// Unit tests for the <see cref="SubmitContext"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class SubmitContextTests
    {
        private readonly IQueryHandler queryHandler;
        private readonly IEdmModel model;
        private readonly ISubmitHandler submitHandler;
        private SubmitContext testClass;
        private ApiBase api;
        private ChangeSet changeSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubmitContextTests"/> class.
        /// </summary>
        public SubmitContextTests()
        {
            queryHandler = Substitute.For<IQueryHandler>();
            model = Substitute.For<IEdmModel>();
            submitHandler = Substitute.For<ISubmitHandler>();
            api = new TestApi(model, queryHandler, submitHandler);
            changeSet = new ChangeSet();
            testClass = new SubmitContext(api, changeSet);
        }

        [Fact]
        public void CanConstruct()
        {
            var instance = new SubmitContext(api, changeSet);
            instance.Should().NotBeNull();
        }

        [Fact]
        public void CannotConstructWithNullApi()
        {
            Action act = () => new SubmitContext(default(ApiBase), new ChangeSet());
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ChangeSetIsInitializedCorrectly()
        {
            testClass.ChangeSet.Should().Be(changeSet);
        }

        [Fact]
        public void CanSetAndGetChangeSet()
        {
            var testValue = new ChangeSet();
            testClass.ChangeSet = testValue;
            testClass.ChangeSet.Should().Be(testValue);
        }

        [Fact]
        public void CannotSetAndGetChangeSetWithResult()
        {
            var testValue = new ChangeSet();
            testClass.ChangeSet = testValue;
            testClass.Result = new SubmitResult(testClass.ChangeSet);
            Action act = () => testClass.ChangeSet = new ChangeSet();
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void CanSetAndGetResult()
        {
            var testValue = new SubmitResult(new Exception());
            testClass.Result = testValue;
            testClass.Result.Should().Be(testValue);
        }

        private class TestApi : ApiBase
        {
            public TestApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }
        }
    }
}