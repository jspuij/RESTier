// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using NSubstitute;
using Xunit;

namespace Microsoft.Restier.Tests.Core
{
    /// <summary>
    /// Unit tests for the <see cref="ConventionBasedChangeSetItemValidator"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ConventionBasedChangeSetItemValidatorTests
    {
        private readonly IQueryHandler queryHandler;
        private readonly IEdmModel model;
        private readonly ISubmitHandler submitHandler;
        private readonly DataModificationItem dataModificationItem;
        private readonly TestTraceListener testTraceListener = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConventionBasedChangeSetItemValidatorTests"/> class.
        /// </summary>
        public ConventionBasedChangeSetItemValidatorTests()
        {
            queryHandler = Substitute.For<IQueryHandler>();
            model = Substitute.For<IEdmModel>();
            submitHandler = Substitute.For<ISubmitHandler>();
            dataModificationItem = new DataModificationItem(
                "Test",
                typeof(object),
                typeof(object),
                RestierEntitySetOperation.Insert,
                new Dictionary<string, object>(),
                new Dictionary<string, object>(),
                new Dictionary<string, object>())
            {
                Resource = new ValidatableEntity()
                {
                    Property = "This is a test",
                    Number = 1,
                },
            };
            Trace.Listeners.Add(testTraceListener);
        }

        /// <summary>
        /// Checks whether the <see cref="ConventionBasedChangeSetItemValidator"/> can be constructed.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var instance = new ConventionBasedChangeSetItemValidator();
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Check that ValidateChangeSetItemAsync can be called.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CanCallValidateChangeSetItemAsync()
        {
            var testClass = new ConventionBasedChangeSetItemValidator();
            var context = new SubmitContext(new EmptyApi(model, queryHandler, submitHandler), new ChangeSet());
            var cancellationToken = CancellationToken.None;

            var validationResults = new Collection<ChangeSetItemValidationResult>();
            await testClass.ValidateChangeSetItemAsync(context, dataModificationItem, validationResults, cancellationToken);
            validationResults.Should().BeEmpty();
        }

        /// <summary>
        /// Make sure that calling ValidateChangeSetItemAsync actually validates the resource.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ValidateChangeSetItemAsyncValidates()
        {
            var testClass = new ConventionBasedChangeSetItemValidator();
            var context = new SubmitContext(new EmptyApi(model, queryHandler, submitHandler), new ChangeSet());
            var cancellationToken = CancellationToken.None;
            dataModificationItem.Resource = new ValidatableEntity()
            {
                Property = null,
                Number = -1,
            };
            var validationResults = new Collection<ChangeSetItemValidationResult>();
            await testClass.ValidateChangeSetItemAsync(context, dataModificationItem, validationResults, cancellationToken);

            validationResults.Should().SatisfyRespectively(
                first =>
                {
                    first.PropertyName.Should().Be(nameof(ValidatableEntity.Property));
                    first.Message.ToUpperInvariant().Should().Contain("REQUIRED");
                },
                second =>
                {
                    second.PropertyName.Should().Be(nameof(ValidatableEntity.Number));
                    second.Message.ToUpperInvariant().Should().Contain("BETWEEN");
                });
        }

        /// <summary>
        /// Checks that ValidateChangeSetItemAsync throws when the submit context is null.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CannotCallValidateChangeSetItemAsyncWithNullContext()
        {
            var testClass = new ConventionBasedChangeSetItemValidator();
            Func<Task> act = () => testClass.ValidateChangeSetItemAsync(
                default(SubmitContext),
                dataModificationItem,
                new Collection<ChangeSetItemValidationResult>(),
                CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Checks that ValidateChangeSetItemAsync throws when the changesetitem is null.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CannotCallValidateChangeSetItemAsyncWithNullItem()
        {
            var testClass = new ConventionBasedChangeSetItemValidator();
            var context = new SubmitContext(new EmptyApi(model, queryHandler, submitHandler), new ChangeSet());
            Func<Task> act = () => testClass.ValidateChangeSetItemAsync(
                context,
                default(ChangeSetItem),
                new Collection<ChangeSetItemValidationResult>(),
                CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Checks that ValidateChangeSetItemAsync throws when the collection of <see cref="ChangeSetItemValidationResult"/> is null.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CannotCallValidateChangeSetItemAsyncWithNullValidationResults()
        {
            var testClass = new ConventionBasedChangeSetItemValidator();
            var context = new SubmitContext(new EmptyApi(model, queryHandler, submitHandler), new ChangeSet());
            Func<Task> act = () => testClass.ValidateChangeSetItemAsync(
                context,
                dataModificationItem,
                default(Collection<ChangeSetItemValidationResult>),
                CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        private class EmptyApi : ApiBase
        {
            public EmptyApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }
        }

        private class ValidatableEntity
        {
            [Required]
            public string Property { get; set; }

            [Range(1, 10)]
            public int Number { get; set; }
        }
    }
}