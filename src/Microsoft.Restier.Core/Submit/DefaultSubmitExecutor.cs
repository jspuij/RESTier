// <copyright file="DefaultSubmitExecutor.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Submit
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Default implementation of <see cref="ISubmitExecutor"/>.
    /// </summary>
    public class DefaultSubmitExecutor : ISubmitExecutor
    {
        /// <inheritdoc />
        public virtual Task<SubmitResult> ExecuteSubmitAsync(SubmitContext context, CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, nameof(context));
            return Task.FromResult(new SubmitResult(context.ChangeSet));
        }
    }
}