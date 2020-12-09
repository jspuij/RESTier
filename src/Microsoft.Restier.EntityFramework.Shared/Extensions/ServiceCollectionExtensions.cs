// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

#if EF7
namespace Microsoft.Restier.EntityFrameworkCore
#else
namespace Microsoft.Restier.EntityFramework
#endif
{
    using System;

#if EF7
    using Microsoft.EntityFrameworkCore;
#else
    using System.Data.Entity;
#endif
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Model;
    using Microsoft.Restier.Core.Query;
    using Microsoft.Restier.Core.Submit;

    /// <summary>
    /// Contains extension methods of <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
#if EF7
        /// <summary>
        /// This method is used to add entity framework providers service into container.
        /// </summary>
        /// <typeparam name="TDbContext">The DbContext type.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>Current <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddEFCoreProviderServices<TDbContext>(this IServiceCollection services)
            where TDbContext : DbContext
        {
            services.TryAddScoped<DbContext>(s => (DbContext)s.GetService(typeof(TDbContext)));

            return AddEFProviderServices<TDbContext>(services);
        }

#else
        /// <summary>
        /// This method is used to add entity framework providers service into container.
        /// </summary>
        /// <typeparam name="TDbContext">The DbContext type.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>Current <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddEF6ProviderServices<TDbContext>(this IServiceCollection services)
            where TDbContext : DbContext
        {
            services.TryAddScoped<DbContext>(sp =>
            {
                var dbContext = Activator.CreateInstance<TDbContext>();
                dbContext.Configuration.ProxyCreationEnabled = false;
                return dbContext;
            });

            return AddEFProviderServices<TDbContext>(services);
        }
#endif

        /// <summary>
        /// This method is used to add entity framework providers service into container.
        /// </summary>
        /// <typeparam name="TDbContext">The DbContext type.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>Current <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddEFProviderServices<TDbContext>(IServiceCollection services)
            where TDbContext : DbContext
        {
            return services
                .AddChainedService<IModelBuilder, EFModelProducer>()
                .AddChainedService<IModelMapper>((sp, next) => new EFModelMapper(typeof(TDbContext)))
                .AddChainedService<IQueryExpressionSourcer, EFQueryExpressionSourcer>()
                .AddChainedService<IQueryExecutor, EFQueryExecutor>()
                .AddChainedService<IQueryExpressionProcessor, EFQueryExpressionProcessor>()
                .AddChainedService<IChangeSetInitializer, EFChangeSetInitializer>()
                .AddChainedService<ISubmitExecutor, EFSubmitExecutor>();
        }
    }
}