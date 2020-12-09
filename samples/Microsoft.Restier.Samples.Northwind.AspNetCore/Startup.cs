using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Samples.Northwind.AspNet.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Restier.Samples.Northwind.AspNetCore
{
    /// <summary>
    /// Startup class. Configures the container and the application.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Configures the container.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<NorthwindContext>(opt => opt.UseSqlServer("data source=(LocalDB)\\MSSQLLocalDB;attachdbfilename=data source=(LocalDB)\\MSSQLLocalDB;attachdbfilename=C:\\source\\Git\\RESTier\\samples\\Microsoft.Restier.Samples.Northwind.AspNetCore\\Data\\Northwind.mdf;integrated security=True;connect timeout=30;MultipleActiveResultSets=True;"));
            services.AddRestier();
            services.AddMvc(options => options.EnableEndpointRouting = false);
        }

        /// <summary>
        /// Configures the application and the HTTP Request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc(builder =>
            {
                builder.Select().Expand().Filter().OrderBy().MaxTop(100).Count();

                builder.MapRestier<NorthwindApi>("ApiV1", "",
                    services => {
                        
                        services.AddDbContext<NorthwindContext>(opt => opt.UseSqlServer("data source=(LocalDB)\\MSSQLLocalDB;attachdbfilename=data source=(LocalDB)\\MSSQLLocalDB;attachdbfilename=C:\\source\\Git\\RESTier\\samples\\Microsoft.Restier.Samples.Northwind.AspNetCore\\Data\\Northwind.mdf;integrated security=True;connect timeout=30;MultipleActiveResultSets=True;"));

                        // This delegate is executed after OData is added to the container.
                        // Add you replacement services here.
                        services.AddEFCoreProviderServices<NorthwindContext>();

                        services.AddSingleton(new ODataValidationSettings
                        {
                            MaxTop = 5,
                            MaxAnyAllExpressionDepth = 3,
                            MaxExpansionDepth = 3,
                        });
                    }, 
                    containerBuilder => {});
            });
        }
    }
}
