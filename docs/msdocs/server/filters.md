# EntitySet Filters

Have you ever wanted to limit the results of a particular query based on the current user, or maybe you only want
to return results that are marked "active"? 

EntitySet Filters allow you to consistently control the shape of the results returned from particular EntitySets,
even across navigation properties. 

## Convention-Based Filtering

Like the rest of RESTier, this is accomplished through a simple convention that
meets the following criteria:

 1. The filter method name must be `OnFilter{EntitySetName}`, where `{EntitySetName}` is the name the target EntitySet.
 2. It must be a `protected internal` method on the implementing `EntityFrameworkApi` class.
 3. It should accept an `IQueryable<T>` parameter and return an `IQueryable<T>` result where `T` is the Entity type.

### Example

```cs
using System.Linq;
using System.Security.Claims;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFrameworkCore;

namespace Microsoft.OData.Service.Sample.Trippin.Api
{

    /// <summary>
    /// Customizations to the EntityFrameworkApi for the TripPin service.
    /// </summary>
    public class TrippinApi : EntityFrameworkApi<TrippinModel>
    {

        public TrippinApi(TrippinModel dbContext, IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(dbContext, model, queryHandler, submitHandler)
        {
        }

        /// <summary>
        /// Filters the People EntitySet to only return people that have Trips.
        /// </summary>
        protected internal IQueryable<Person> OnFilterPeople(IQueryable<Person> entitySet)
            => entitySet.Where(c => c.Trips.Any());

        /// <summary>
        /// Filters the Trips EntitySet to only return the current user's Trips.
        /// </summary>
        protected internal IQueryable<Trip> OnFilterTrips(IQueryable<Trip> entitySet)
            => entitySet.Where(c => c.PersonId == ClaimsPrincipal.Current.FindFirst("currentUserId").Value);

    }

}
```

> **Note:** In ASP.NET Core, `ClaimsPrincipal.Current` is not automatically populated. To use it in your
> filter methods, add the `UseClaimsPrincipals()` middleware in your `Program.cs`:
>
> ```cs
> app.UseClaimsPrincipals();
> ```
>
> This registers RESTier's `RestierClaimsPrincipalMiddleware`, which sets `ClaimsPrincipal.Current` from
> the current `HttpContext.User` on each request.
