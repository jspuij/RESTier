
    using Microsoft.Restier.EntityFramework;
#if EF6
    using System;
    using System.Data.Common;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Runtime.InteropServices;
    using Microsoft.Extensions.Configuration;
#endif
#if EFCore
using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel;
#endif

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EFServiceCollectionExtensions
    {

#if EF6

        private static IConfiguration _configuration;

        /// <summary>
        /// Gets the test configuration, loading user secrets if available.
        /// </summary>
        private static IConfiguration Configuration
        {
            get
            {
                if (_configuration is null)
                {
                    _configuration = new ConfigurationBuilder()
                        .AddUserSecrets(typeof(EFServiceCollectionExtensions).Assembly, optional: true)
                        .Build();
                }
                return _configuration;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TDbContext"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddEntityFrameworkServices<TDbContext>(this IServiceCollection services) where TDbContext : DbContext
        {
            var connectionString = Configuration.GetConnectionString(typeof(TDbContext).Name);

            if (!string.IsNullOrEmpty(connectionString))
            {
                // Append the runtime version to the database name so that parallel TFM test runs
                // (e.g. net8.0 and net9.0) don't collide on the same database.
                var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
                if (builder.ContainsKey("Initial Catalog"))
                {
                    builder["Initial Catalog"] = $"{builder["Initial Catalog"]}_{Environment.Version.Major}";
                }
                else if (builder.ContainsKey("Database"))
                {
                    builder["Database"] = $"{builder["Database"]}_{Environment.Version.Major}";
                }

                return services.AddEF6ProviderServices<TDbContext>(builder.ConnectionString);
            }

            return services.AddEF6ProviderServices<TDbContext>();
        }

#endif

#if EFCore

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TDbContext"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddEntityFrameworkServices<TDbContext>(this IServiceCollection services) where TDbContext : DbContext
        {
            services.AddEFCoreProviderServices<TDbContext>();

            if (typeof(TDbContext) == typeof(LibraryContext))
            {
                services.SeedDatabase<LibraryContext, LibraryTestInitializer>();
            }
            else if (typeof(TDbContext) == typeof(MarvelContext))
            {
                services.SeedDatabase<MarvelContext, MarvelTestInitializer>();
            }

            return services;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TInitializer"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static void SeedDatabase<TContext, TInitializer>(this IServiceCollection services)
            where TContext : DbContext
            where TInitializer : IDatabaseInitializer, new()
        {
            using var tempServices = services.BuildServiceProvider();

            var scopeFactory = tempServices.GetService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<TContext>();

            // EnsureCreated() returns false if the database already exists
            if (dbContext.Database.EnsureCreated())
            {
                var initializer = new TInitializer();
                initializer.Seed(dbContext);
            }

        }

#endif

    }

}
