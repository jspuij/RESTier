// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Data.Entity.Spatial;
using FluentAssertions;
using Microsoft.Restier.Core.Spatial;
using Microsoft.Restier.EntityFramework;
using Microsoft.Spatial;
using NSubstitute;
using Xunit;

namespace Microsoft.Restier.Tests.EntityFramework.Spatial
{
    public class EFChangeSetInitializerSpatialTests
    {
        public static bool SqlServerTypesAvailable
        {
            get
            {
                try
                {
                    _ = DbSpatialServices.Default;
                    DbGeography.FromText("POINT(0 0)", 4326);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        [Fact(Skip = "Requires Microsoft.SqlServer.Types native assembly (Windows / SQL Server only).",
              SkipUnless = nameof(SqlServerTypesAvailable))]
        public void ConvertToEfValue_dispatches_to_registered_spatial_converter_for_DbGeography()
        {
            var fakeDbg = DbGeography.FromText("POINT(1 2)", 4326);
            var fakeEdm = GeographyPoint.Create(
                CoordinateSystem.Geography(4326), 2, 1, null, null);

            var converter = Substitute.For<ISpatialTypeConverter>();
            converter.CanConvert(typeof(DbGeography)).Returns(true);
            converter.ToStorage(typeof(DbGeography), fakeEdm).Returns(fakeDbg);

            var initializer = new EFChangeSetInitializer(new[] { converter });
            var result = initializer.ConvertToEfValue(typeof(DbGeography), fakeEdm);

            result.Should().BeSameAs(fakeDbg);
        }

        [Fact]
        public void ConvertToEfValue_passes_through_when_no_converter_registered()
        {
            var initializer = new EFChangeSetInitializer();
            var result = initializer.ConvertToEfValue(typeof(int), 42);
            result.Should().Be(42);
        }
    }
}
