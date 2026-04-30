// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using System.Linq;

namespace Microsoft.Restier.Tests.AspNetCore.NSwag.Infrastructure
{

    public class TestApi : ApiBase
    {

        public TestApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(model, queryHandler, submitHandler)
        {
        }

        public IQueryable<TestEntity> Items => Enumerable.Empty<TestEntity>().AsQueryable();

    }

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public static class TestEdmModelBuilder
    {
        public static IEdmModel Build()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<TestEntity>(nameof(TestApi.Items));
            return builder.GetEdmModel();
        }
    }

}
