// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Core.DependencyInjection
{
    /// <summary>
    /// Interface implemented by factories that create a chain of responsibility.
    /// </summary>
    /// <typeparam name="T">The service type to create.</typeparam>
    public interface IChainOfResponsibilityFactory<T> where T : class, IChainedService<T>
    {
        /// <summary>
        /// Creates a chain of responsibility.
        /// </summary>
        /// <returns>The chained service of type <typeparamref name="T"/></returns>
        T Create();
    }
}