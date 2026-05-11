// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.Core.Spatial;
using Microsoft.Restier.EntityFramework.Shared.Model;
using Microsoft.Restier.EntityFrameworkCore.Spatial;
using Microsoft.Spatial;
using Xunit;

namespace Microsoft.Restier.Tests.EntityFrameworkCore.Spatial
{
    public class SpatialModelConventionTests
    {
        private class City
        {
            public int Id { get; set; }

            public NetTopologySuite.Geometries.Point HeadquartersLocation { get; set; }

            [Spatial(typeof(GeometryPoint))]
            public NetTopologySuite.Geometries.Point IndoorOrigin { get; set; }
        }

        private class CityContext : DbContext
        {
            public DbSet<City> Cities { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                => optionsBuilder.UseInMemoryDatabase("convention-tests");

            protected override void OnModelCreating(ModelBuilder b)
            {
                b.Entity<City>(e =>
                {
                    e.Property(x => x.HeadquartersLocation).HasColumnType("geography");
                });
            }
        }

        [Fact]
        public void Phase1_captures_spatial_properties_with_resolved_edm_types()
        {
            using var ctx = new CityContext();
            var providers = new ISpatialModelMetadataProvider[] { new NtsSpatialModelMetadataProvider() };
            var convention = new SpatialModelConvention(providers);
            var builder = new ODataConventionModelBuilder { Namespace = "Test" };
            builder.EntitySet<City>("Cities");

            var captures = convention.CapturePhase(builder, new[] { typeof(City) }, ctx);

            captures.Should().HaveCount(2);
            captures.Should().Contain(c => c.PropertyInfo.Name == nameof(City.HeadquartersLocation) && c.ResolvedEdmType == typeof(GeographyPoint));
            captures.Should().Contain(c => c.PropertyInfo.Name == nameof(City.IndoorOrigin) && c.ResolvedEdmType == typeof(GeometryPoint));
        }

        [Fact]
        public void Phase1_calls_Ignore_for_storage_types()
        {
            using var ctx = new CityContext();
            var providers = new ISpatialModelMetadataProvider[] { new NtsSpatialModelMetadataProvider() };
            var convention = new SpatialModelConvention(providers);
            var builder = new ODataConventionModelBuilder { Namespace = "Test" };
            builder.EntitySet<City>("Cities");

            convention.CapturePhase(builder, new[] { typeof(City) }, ctx);

            var model = builder.GetEdmModel();
            var cityType = model.FindDeclaredType("Test.City") as IEdmStructuredType;
            cityType.Should().NotBeNull();
            cityType.DeclaredProperties.Select(p => p.Name)
                .Should().NotContain(new[] { nameof(City.HeadquartersLocation), nameof(City.IndoorOrigin) });
        }
    }
}
