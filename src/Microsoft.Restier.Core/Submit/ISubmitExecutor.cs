// <copyright file="ISubmitExecutor.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Submit
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a service that executes a submission.
    /// </summary>
    public interface ISubmitExecutor
    {
        /// <summary>
        /// Asynchronously executes a submission and produces a submit result.
        /// </summary>
        /// <param name="context">
        /// The submit context.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is a submit result.
        /// </returns>
        Task<SubmitResult> ExecuteSubmitAsync(
            SubmitContext context,
            CancellationToken cancellationToken);
    }
}
