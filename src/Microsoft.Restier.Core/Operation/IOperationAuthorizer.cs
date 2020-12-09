// <copyright file="IOperationAuthorizer.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Operation
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Restier.Core.Submit;

    /// <summary>
    /// Represents a operation authorizer.
    /// </summary>
    public interface IOperationAuthorizer
    {
        /// <summary>
        /// Asynchronously authorizes the Operation.
        /// </summary>
        /// <param name="context">
        /// The operation context.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task<bool> AuthorizeAsync(
            OperationContext context,
            CancellationToken cancellationToken);
    }
}
