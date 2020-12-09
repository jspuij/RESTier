// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.AspNet.Extensions
{
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Model;
    using Microsoft.Restier.Core.Query;
    using Microsoft.Restier.Core.Submit;
    using Microsoft.Restier.Tests.Shared;
    using Microsoft.Restier.Tests.Shared.AspNet.Scenarios.Store;

    /// <summary>
    /// Extensions to <see cref="IServiceCollection"/> that make registering services for Restier Tests easier.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the required <see cref="StoreApi"/> services to an <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The service collection to use.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddTestStoreApiServices(this IServiceCollection services)
        {
            services.AddChainedService<IModelBuilder>((sp, next) => new StoreModelProducer(StoreModel.Model))
                .AddChainedService<IModelMapper>((sp, next) => new StoreModelMapper())
                .AddChainedService<IQueryExpressionSourcer>((sp, next) => new StoreQueryExpressionSourcer())
                .AddChainedService<IChangeSetInitializer>((sp, next) => new StoreChangeSetInitializer())
                .AddChainedService<ISubmitExecutor>((sp, next) => new DefaultSubmitExecutor());
            return services;
        }
    }
}