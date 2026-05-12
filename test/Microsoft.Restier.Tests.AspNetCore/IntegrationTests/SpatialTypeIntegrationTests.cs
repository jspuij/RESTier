// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.IntegrationTests;

/// <summary>
/// End-to-end integration tests that verify spatial type support in the Library API.
/// These tests exercise the EDM model metadata and HTTP query surface for the
/// <c>SpatialPlaces</c> entity set introduced in H1.
/// </summary>
[Collection("LibraryApiEFCore")]
public class SpatialTypeIntegrationTests : RestierTestBase<LibraryApi>
{
    private readonly Action<IServiceCollection> _configureServices
        = services => services.AddEntityFrameworkServices<LibraryContext>();

    // ─────────────────────────────────────────────────────────────────────────
    // EDM / metadata assertions (EFCore)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EFCore: $metadata must expose a SpatialPlace EntityType with the expected Edm spatial types.
    /// HeadquartersLocation and IndoorOrigin are NTS Point columns with HasColumnType("geography"),
    /// so they resolve to Edm.GeographyPoint.  ServiceArea is an NTS Polygon geography column,
    /// so it resolves to Edm.GeographyPolygon.
    /// </summary>
    [Fact]
    public async Task EFCore_Metadata_SpatialPlace_HasCorrectEdmTypes()
    {
        var metadata = await RestierTestHelpers.GetApiMetadataAsync<LibraryApi>(
            serviceCollection: _configureServices);

        metadata.Should().NotBeNull("$metadata request must succeed");

        var xml = metadata.ToString();

        xml.Should().Contain(
            "EntityType Name=\"SpatialPlace\"",
            "SpatialPlace must be an EntityType in the EDM");

        xml.Should().Contain(
            "Name=\"HeadquartersLocation\" Type=\"Edm.GeographyPoint\"",
            "NTS Point with HasColumnType(\"geography\") must map to Edm.GeographyPoint");

        xml.Should().Contain(
            "Name=\"ServiceArea\" Type=\"Edm.GeographyPolygon\"",
            "NTS Polygon with HasColumnType(\"geography\") must map to Edm.GeographyPolygon");

        xml.Should().Contain(
            "Name=\"IndoorOrigin\" Type=\"Edm.GeographyPoint\"",
            "[Spatial(typeof(GeographyPoint))] with geography column type must map to Edm.GeographyPoint");

        xml.Should().Contain(
            "EntitySet Name=\"SpatialPlaces\"",
            "SpatialPlaces EntitySet must be exposed in the EDM container");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HTTP GET — collection and single-entity
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EFCore: GET /SpatialPlaces must return 200 OK (entity set is routable).
    /// </summary>
    [Fact]
    public async Task EFCore_Get_SpatialPlaces_Returns200()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/SpatialPlaces",
            serviceCollection: _configureServices);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// EFCore: GET /SpatialPlaces(1) must return 200 OK.
    /// The seeded record always exists (spatial values may be null when SQL CLR is disabled,
    /// but the record itself is always inserted).
    /// </summary>
    [Fact]
    public async Task EFCore_Get_SpatialPlaces_ByKey_Returns200()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/SpatialPlaces(1)",
            serviceCollection: _configureServices);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "the seeded SpatialPlace record (Id=1) must be retrievable via key lookup");

        content.Should().Contain("\"Id\":1",
            "the returned entity must have the expected Id");

        content.Should().Contain("\"Name\":\"Spatial Place 1\"",
            "the returned entity must have the expected Name");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Negative test — geo.distance $filter (spec-A limitation)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EFCore: $filter using geo.distance must NOT return 200.  EF Core + NTS does not translate
    /// the OData geo.distance function, so the request must be rejected (4xx) or cause a server
    /// error (5xx).  This test locks the documented spec-A limitation.
    /// </summary>
    [Fact]
    public async Task EFCore_Filter_GeoDistance_IsNotTranslatable_ReturnsError()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/SpatialPlaces?$filter=geo.distance(HeadquartersLocation,geography'POINT(0 0)') lt 10000",
            serviceCollection: _configureServices);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeFalse(
            "geo.distance translation is not supported by EF Core + NTS (spec-A limitation); " +
            "the server must return a 4xx or 5xx error, not a successful response");
    }
}
