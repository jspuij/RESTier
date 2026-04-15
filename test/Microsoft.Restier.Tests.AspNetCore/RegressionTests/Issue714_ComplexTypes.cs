// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.Breakdance.AspNetCore;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.RegressionTests;

/// <summary>
/// Regression tests for https://github.com/OData/RESTier/issues/714.
/// </summary>
public class Issue714_ComplexTypes : RestierTestBase<ComplexTypesApi>
{
    public Issue714_ComplexTypes()
    {
        AddRestierAction = options =>
        {
            options.AddRestierRoute<ComplexTypesApi>(WebApiConstants.RoutePrefix, routeServices =>
            {
                routeServices
                    .AddEntityFrameworkServices<MarvelContext>()
                    .AddSingleton<IChainedService<IModelBuilder>, ComplexTypesModelBuilder>();
            });
        };
        TestSetup();
    }

    [Fact]
    public async Task ComplexTypes_WorkAsExpected()
    {
        var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/ComplexTypeTest()");
        response.Should().NotBeNull();

        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        content.Should().NotBeNullOrWhiteSpace();
    }
}

#region ComplexTypesApi

public class ComplexTypesApi : MarvelApi
{
    public ComplexTypesApi(MarvelContext dbContext, IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
        : base(dbContext, model, queryHandler, submitHandler)
    {
    }

    [UnboundOperation(OperationType = OperationType.Function)]
    public LibraryCard ComplexTypeTest()
    {
        return new()
        {
            Id = Guid.NewGuid()
        };
    }
}

#endregion

#region ComplexTypesModelBuilder

/// <summary>
/// Builds the EdmModel for the Restier API.
/// </summary>
/// <remarks>
/// Hopefully this won't be necessary if we can get the OperationAttribute to register types it does not recognize.
/// </remarks>
public class ComplexTypesModelBuilder : IModelBuilder
{
    public IEdmModel GetEdmModel()
    {
        var modelBuilder = new ODataConventionModelBuilder();
        modelBuilder.ComplexType<LibraryCard>();
        return modelBuilder.GetEdmModel();
    }

    public IModelBuilder Inner { get; set; }
}

#endregion
