// <copyright file="DefaultChangeSetInitializer.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Submit
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides a default implementation of the <see cref="IChangeSetInitializer"/> interface.
    /// </summary>
    public class DefaultChangeSetInitializer : IChangeSetInitializer
    {
        /// <inheritdoc />
        public virtual Task InitializeAsync(SubmitContext context, CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, nameof(context));
            context.ChangeSet = new ChangeSet();
            return Task.CompletedTask;
        }
    }
}