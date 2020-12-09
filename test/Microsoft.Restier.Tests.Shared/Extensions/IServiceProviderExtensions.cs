// <copyright file="IServiceProviderExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.Extensions
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Restier.Core;

    /// <summary>
    /// Extension methods for the <see cref="IServiceProvider"/> interface.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class IServiceProviderExtensions
    {
        /// <summary>
        /// Gets a testable api instance from the container.
        /// </summary>
        /// <typeparam name="T">The Api type.</typeparam>
        /// <param name="serviceProvider">The service provider that will provide the services.</param>
        /// <returns>A testable api instance.</returns>
        public static T GetTestableApiInstance<T>(this IServiceProvider serviceProvider)
            where T : ApiBase => serviceProvider.GetService<T>();
    }
}