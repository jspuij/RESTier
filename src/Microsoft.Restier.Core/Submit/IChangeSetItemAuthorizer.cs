// <copyright file="IChangeSetItemAuthorizer.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Submit
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a change set item authorizer.
    /// </summary>
    public interface IChangeSetItemAuthorizer
    {
        /// <summary>
        /// Asynchronously authorizes the ChangeSetItem.
        /// </summary>
        /// <param name="context">
        /// The submit context.
        /// </param>
        /// <param name="item">
        /// A change set item to be authorized.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task<bool> AuthorizeAsync(
            SubmitContext context,
            ChangeSetItem item,
            CancellationToken cancellationToken);
    }
}
