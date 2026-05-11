// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Spatial;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Model
{
    public class EdmHelpersSpatialTests
    {
        [Theory]
        [InlineData(typeof(GeographyPoint), EdmPrimitiveTypeKind.GeographyPoint)]
        [InlineData(typeof(GeographyLineString), EdmPrimitiveTypeKind.GeographyLineString)]
        [InlineData(typeof(GeographyPolygon), EdmPrimitiveTypeKind.GeographyPolygon)]
        [InlineData(typeof(GeographyMultiPoint), EdmPrimitiveTypeKind.GeographyMultiPoint)]
        [InlineData(typeof(GeographyMultiLineString), EdmPrimitiveTypeKind.GeographyMultiLineString)]
        [InlineData(typeof(GeographyMultiPolygon), EdmPrimitiveTypeKind.GeographyMultiPolygon)]
        [InlineData(typeof(GeographyCollection), EdmPrimitiveTypeKind.GeographyCollection)]
        [InlineData(typeof(Geography), EdmPrimitiveTypeKind.Geography)]
        [InlineData(typeof(GeometryPoint), EdmPrimitiveTypeKind.GeometryPoint)]
        [InlineData(typeof(GeometryLineString), EdmPrimitiveTypeKind.GeometryLineString)]
        [InlineData(typeof(GeometryPolygon), EdmPrimitiveTypeKind.GeometryPolygon)]
        [InlineData(typeof(GeometryMultiPoint), EdmPrimitiveTypeKind.GeometryMultiPoint)]
        [InlineData(typeof(GeometryMultiLineString), EdmPrimitiveTypeKind.GeometryMultiLineString)]
        [InlineData(typeof(GeometryMultiPolygon), EdmPrimitiveTypeKind.GeometryMultiPolygon)]
        [InlineData(typeof(GeometryCollection), EdmPrimitiveTypeKind.GeometryCollection)]
        [InlineData(typeof(Geometry), EdmPrimitiveTypeKind.Geometry)]
        public void GetPrimitiveTypeReference_recognizes_Microsoft_Spatial_types(Type clrType, EdmPrimitiveTypeKind expected)
        {
            var reference = clrType.GetPrimitiveTypeReference();
            reference.Should().NotBeNull();
            reference.PrimitiveKind().Should().Be(expected);
        }
    }
}
