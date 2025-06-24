// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;

#if NET6_0_OR_GREATER
    using Microsoft.Restier.AspNetCore.Model;
#else
    using Microsoft.Restier.AspNet.Model;
#endif

#if EF6
    using Microsoft.Restier.EntityFramework;
#endif
#if EFCore
    using Microsoft.Restier.EntityFrameworkCore;
#endif

namespace Microsoft.Restier.Tests.Shared.Scenarios.Marvel
{

    /// <summary>
    /// A testable API that implements an Entity Framework model and has secondary operations
    /// </summary>
    public class MarvelApi : EntityFrameworkApi<MarvelContext>
    {

        public MarvelApi(MarvelContext dbContext, IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(dbContext, model, queryHandler, submitHandler)
        {
        }

    }

}