// <copyright file="DefaultSubmitHandler.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Submit
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.Tracing;
    using System.Globalization;
    using System.Linq;
    using System.Security;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The default handler for submitting changes through the <see cref="ApiBase"/>.
    /// </summary>
    internal class DefaultSubmitHandler
    {
        private readonly IChangeSetInitializer initializer;
        private readonly IChangeSetItemAuthorizer authorizer;
        private readonly IChangeSetItemValidator validator;
        private readonly IChangeSetItemFilter filter;
        private readonly ISubmitExecutor executor;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultSubmitHandler"/> class.
        /// </summary>
        /// <param name="initializer">A reference to a service that can initialize a change set.</param>
        /// <param name="executor">A reference to a service that executes a submission.</param>
        /// <param name="authorizer">An optional reference to an service that authorizes a submission.</param>
        /// <param name="validator">An optional reference to a service that validates a submission.</param>
        /// <param name="filter">An optional reference to a service that executes logic before and after a submission.</param>
        public DefaultSubmitHandler(
            IChangeSetInitializer initializer,
            ISubmitExecutor executor,
            IChangeSetItemAuthorizer authorizer = null,
            IChangeSetItemValidator validator = null,
            IChangeSetItemFilter filter = null)
        {
            Ensure.NotNull(initializer, nameof(initializer));
            Ensure.NotNull(executor, nameof(executor));

            this.initializer = initializer;
            this.authorizer = authorizer;
            this.validator = validator;
            this.filter = filter;
            this.executor = executor;
        }

        /// <summary>
        /// Asynchronously executes the submit flow.
        /// </summary>
        /// <param name="context">The submit context.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>
        /// A task that represents the asynchronous operation whose result is a submit result.
        /// </returns>
        public async Task<SubmitResult> SubmitAsync(SubmitContext context, CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, nameof(context));

            await this.initializer.InitializeAsync(context, cancellationToken).ConfigureAwait(false);

            if (context.Result != null)
            {
                return context.Result;
            }
            else if (context.ChangeSet == null)
            {
                throw new InvalidOperationException(Resources.NoPendingChangedAndNoChangeSet);
            }

            var eventsChangeSet = context.ChangeSet;

            IEnumerable<ChangeSetItem> currentChangeSetItems = eventsChangeSet.Entries.ToArray();

            await this.PerformValidate(context, currentChangeSetItems, cancellationToken).ConfigureAwait(false);

            await this.PerformPreEvent(context, currentChangeSetItems, cancellationToken).ConfigureAwait(false);

            await this.PerformPersist(context, cancellationToken).ConfigureAwait(false);

            context.ChangeSet.Entries.Clear();

            await this.PerformPostEvent(context, currentChangeSetItems, cancellationToken).ConfigureAwait(false);

            return context.Result;
        }

        private static string GetAuthorizeFailedMessage(ChangeSetItem item)
        {
            switch (item.Type)
            {
                case ChangeSetItemType.DataModification:
                    var dataModification = (DataModificationItem)item;
                    string message = null;
                    if (dataModification.EntitySetOperation == RestierEntitySetOperation.Insert)
                    {
                        message = Resources.NoPermissionToInsertEntity;
                    }
                    else if (dataModification.EntitySetOperation == RestierEntitySetOperation.Update)
                    {
                        message = Resources.NoPermissionToUpdateEntity;
                    }
                    else if (dataModification.EntitySetOperation == RestierEntitySetOperation.Delete)
                    {
                        message = Resources.NoPermissionToDeleteEntity;
                    }
                    else
                    {
                        throw new NotSupportedException(Resources.DataModificationMustBeCUD);
                    }

                    return string.Format(CultureInfo.InvariantCulture, message, dataModification.ResourceSetName);

                default:
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidChangeSetEntryType, item.Type));
            }
        }

        private async Task PerformValidate(SubmitContext context, IEnumerable<ChangeSetItem> changeSetItems, CancellationToken cancellationToken)
        {
            await this.InvokeAuthorizers(context, changeSetItems, cancellationToken).ConfigureAwait(false);

            await this.InvokeValidators(context, changeSetItems, cancellationToken).ConfigureAwait(false);

            foreach (var item in changeSetItems.Where(i => i.HasChanged()))
            {
                if (item.ChangeSetItemProcessingStage == ChangeSetItemProcessingStage.ChangedWithinOwnPreEventing)
                {
                    item.ChangeSetItemProcessingStage = ChangeSetItemProcessingStage.PreEvented;
                }
                else
                {
                    item.ChangeSetItemProcessingStage = ChangeSetItemProcessingStage.Validated;
                }
            }
        }

        private async Task InvokeAuthorizers(SubmitContext context, IEnumerable<ChangeSetItem> changeSetItems, CancellationToken cancellationToken)
        {
            if (this.authorizer == null)
            {
                return;
            }

            foreach (var item in changeSetItems.Where(i => i.HasChanged()))
            {
                if (!await this.authorizer.AuthorizeAsync(context, item, cancellationToken).ConfigureAwait(false))
                {
                    throw new SecurityException(GetAuthorizeFailedMessage(item));
                }
            }
        }

        private async Task InvokeValidators(SubmitContext context, IEnumerable<ChangeSetItem> changeSetItems, CancellationToken cancellationToken)
        {
            if (this.validator == null)
            {
                return;
            }

            var validationResults = new Collection<ChangeSetItemValidationResult>();

            foreach (var entry in changeSetItems.Where(i => i.HasChanged()))
            {
                await this.validator.ValidateChangeSetItemAsync(context, entry, validationResults, cancellationToken).ConfigureAwait(false);
            }

            var errors = validationResults.Where(result => result.Severity == EventLevel.Error);

            if (errors.Any())
            {
                var validationErrorMessage = Resources.ValidationFailsTheOperation;
                throw new ChangeSetValidationException(validationErrorMessage)
                {
                    ValidationResults = errors,
                };
            }
        }

        private async Task PerformPreEvent(SubmitContext context, IEnumerable<ChangeSetItem> changeSetItems, CancellationToken cancellationToken)
        {
            if (this.filter == null)
            {
                return;
            }

            foreach (var item in changeSetItems)
            {
                if (item.ChangeSetItemProcessingStage == ChangeSetItemProcessingStage.Validated)
                {
                    item.ChangeSetItemProcessingStage = ChangeSetItemProcessingStage.PreEventing;

                    if (this.filter != null)
                    {
                        await this.filter.OnChangeSetItemProcessingAsync(context, item, cancellationToken).ConfigureAwait(false);
                    }

                    if (item.ChangeSetItemProcessingStage == ChangeSetItemProcessingStage.PreEventing)
                    {
                        // if the state is still the intermediate state,
                        // the entity was not changed during processing
                        // and can move to the next step
                        item.ChangeSetItemProcessingStage = ChangeSetItemProcessingStage.PreEvented;
                    }
                    else if (item.ChangeSetItemProcessingStage == ChangeSetItemProcessingStage.Initialized /*&&
                        entity.Details.EntityState == originalEntityState*/)
                    {
                        item.ChangeSetItemProcessingStage = ChangeSetItemProcessingStage.ChangedWithinOwnPreEventing;
                    }
                }
            }
        }

        private async Task PerformPersist(SubmitContext context, CancellationToken cancellationToken)
        {
            context.Result = await this.executor.ExecuteSubmitAsync(context, cancellationToken).ConfigureAwait(false);
        }

        private async Task PerformPostEvent(SubmitContext context, IEnumerable<ChangeSetItem> changeSetItems, CancellationToken cancellationToken)
        {
            if (this.filter == null)
            {
                return;
            }

            foreach (var item in changeSetItems)
            {
                await this.filter.OnChangeSetItemProcessedAsync(context, item, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}