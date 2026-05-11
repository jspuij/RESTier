// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Data.Entity.Spatial;
using FluentAssertions;
using Microsoft.Restier.EntityFramework.Spatial;
using Microsoft.Spatial;
using Xunit;

namespace Microsoft.Restier.Tests.EntityFramework.Spatial
{
    public class DbSpatialConverterTests
    {
        /// <summary>
        /// Returns true when the native <c>Microsoft.SqlServer.Types</c> assembly can be loaded
        /// by EF6's spatial loader.  On non-Windows hosts (or machines without SQL Server native
        /// types installed) the three geometry-exercising tests are skipped rather than failing.
        /// </summary>
        public static bool SqlServerTypesAvailable
        {
            get
            {
                try
                {
                    // Force EF6 to probe for the native types assembly now.
                    _ = DbSpatialServices.Default;
                    DbGeography.FromText("POINT(0 0)", 4326);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        private readonly DbSpatialConverter _converter = new();

        [Fact]
        public void CanConvert_returns_true_for_DbGeography()
        {
            _converter.CanConvert(typeof(DbGeography)).Should().BeTrue();
        }

        [Fact(Skip = "Requires Microsoft.SqlServer.Types native assembly (Windows / SQL Server only).",
              SkipUnless = nameof(SqlServerTypesAvailable))]
        public void ToEdm_returns_GeographyPoint_for_DbGeography_Point()
        {
            var dbg = DbGeography.FromText("POINT(4.9041 52.3676)", 4326);

            var result = _converter.ToEdm(dbg, typeof(GeographyPoint));

            var point = result.Should().BeOfType<GeographyPoint>().Subject;
            point.Latitude.Should().BeApproximately(52.3676, 0.0001);
            point.Longitude.Should().BeApproximately(4.9041, 0.0001);
            point.CoordinateSystem.EpsgId.Should().Be(4326);
        }

        [Fact(Skip = "Requires Microsoft.SqlServer.Types native assembly (Windows / SQL Server only).",
              SkipUnless = nameof(SqlServerTypesAvailable))]
        public void ToStorage_returns_DbGeography_for_GeographyPoint()
        {
            var p = GeographyPoint.Create(CoordinateSystem.Geography(4326), 52.3676, 4.9041, null, null);

            var result = _converter.ToStorage(typeof(DbGeography), p);

            var dbg = result.Should().BeOfType<DbGeography>().Subject;
            dbg.SpatialTypeName.Should().Be("Point");
            dbg.Latitude.Should().BeApproximately(52.3676, 0.0001);
            dbg.Longitude.Should().BeApproximately(4.9041, 0.0001);
            dbg.CoordinateSystemId.Should().Be(4326);
        }

        [Fact(Skip = "Requires Microsoft.SqlServer.Types native assembly (Windows / SQL Server only).",
              SkipUnless = nameof(SqlServerTypesAvailable))]
        public void Round_trip_preserves_value()
        {
            var original = DbGeography.FromText("POINT(4.9041 52.3676)", 4326);

            var edm = _converter.ToEdm(original, typeof(GeographyPoint));
            var roundTrip = (DbGeography)_converter.ToStorage(typeof(DbGeography), edm);

            roundTrip.SpatialEquals(original).Should().BeTrue();
            roundTrip.CoordinateSystemId.Should().Be(original.CoordinateSystemId);
        }
    }
}
