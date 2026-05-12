#if EF6
    using Microsoft.Restier.EntityFramework;
    using System;
    using System.Data.Common;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Runtime.InteropServices;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;
    using Microsoft.Restier.Tests.Shared.Scenarios.Marvel.EF6;
#endif
#if EFCore
using System;
using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.EntityFrameworkCore.Spatial;
using Microsoft.Restier.Tests.Shared.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel.EFCore;
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

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException($"Connection string 'ConnectionStrings:{typeof(TDbContext).Name}' is required. Add it with dotnet user-secrets.");
            }

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

            services.AddEF6ProviderServices<TDbContext>(builder.ConnectionString);
            Microsoft.Restier.EntityFramework.Spatial.ServiceCollectionExtensions.AddRestierSpatial(services);
            return services;
        }

#endif

#if EFCore

        private static IConfiguration _configuration;
        private static readonly ConcurrentDictionary<string, object> DatabaseLocks = new();
        private static readonly ConcurrentDictionary<string, bool> InitializedDatabases = new();

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
        /// Adds Entity Framework Core provider services for the specified DbContext.
        /// Uses the SQL Server connection string configured in user secrets.
        /// </summary>
        /// <typeparam name="TDbContext">The type of the DbContext.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddEntityFrameworkServices<TDbContext>(this IServiceCollection services) where TDbContext : DbContext
        {
            var connectionString = Configuration.GetConnectionString(typeof(TDbContext).Name);

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException($"Connection string 'ConnectionStrings:{typeof(TDbContext).Name}' is required. Add it with dotnet user-secrets.");
            }

            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            if (builder.ContainsKey("Initial Catalog"))
            {
                builder["Initial Catalog"] = $"{builder["Initial Catalog"]}_{Environment.Version.Major}_EFCore";
            }
            else if (builder.ContainsKey("Database"))
            {
                builder["Database"] = $"{builder["Database"]}_{Environment.Version.Major}_EFCore";
            }

            services.AddEFCoreProviderServices<TDbContext>(options =>
                options.UseSqlServer(builder.ConnectionString, o => o.UseNetTopologySuite()));
            services.AddRestierSpatial();

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

            var databaseKey = dbContext.Database.IsRelational()
                ? dbContext.Database.GetConnectionString()
                : $"{dbContext.Database.ProviderName}:{typeof(TContext).FullName}";
            var databaseLock = DatabaseLocks.GetOrAdd(databaseKey, _ => new object());
            lock (databaseLock)
            {
                if (!InitializedDatabases.ContainsKey(databaseKey))
                {
                    dbContext.Database.EnsureDeleted();
                    dbContext.Database.EnsureCreated();

                    var initializer = new TInitializer();
                    initializer.Seed(dbContext);
                    InitializedDatabases[databaseKey] = true;
                }
            }

        }

#endif

    }

}
