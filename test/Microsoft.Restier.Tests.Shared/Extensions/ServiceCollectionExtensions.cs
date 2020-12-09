// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.Extensions
{
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Submit;

    /// <summary>
    /// Extension class for service registration.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the default test services to an <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The service collection to use.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddTestDefaultServices(this IServiceCollection services)
        {
            services.AddChainedService<IChangeSetInitializer>((sp, next) => new DefaultChangeSetInitializer())
                .AddChainedService<ISubmitExecutor>((sp, next) => new DefaultSubmitExecutor());
            return services;
        }
    }
}
