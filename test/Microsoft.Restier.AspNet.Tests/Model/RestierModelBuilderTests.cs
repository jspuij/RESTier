// <copyright file="RestierModelBuilderTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.AspNet.Tests.Model
{
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.OData.Edm;
    using Microsoft.OData.Edm.Validation;
    using Microsoft.Restier.Tests.Shared.AspNet;
    using Microsoft.Restier.Tests.Shared.AspNet.Scenarios.Library;
    using Microsoft.Restier.Tests.Shared.EntityFramework.Scenarios.Library;
    using Xunit;

    /// <summary>
    /// Unit tests for the Restier Model Builder.
    /// </summary>
    public class RestierModelBuilderTests
    {
        /// <summary>
        /// Tests that building a model with a complex type should work.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ComplexTypeShoudWork()
        {
            var model = await RestierTestHelpers.GetTestableModelAsync<LibraryApi, LibraryContext>();

            model.Validate(out var errors).Should().BeTrue();
            errors.Should().BeEmpty();

            var address = model.FindDeclaredType("Microsoft.Restier.Tests.Shared.Scenarios.Library.Address") as IEdmComplexType;
            address.Should().NotBeNull();
            address.Properties().Count().Should().Be(2);
        }

        /// <summary>
        /// Check that primitive types should work.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task PrimitiveTypesShouldWork()
        {
            var model = await RestierTestHelpers.GetTestableModelAsync<LibraryApi, LibraryContext>();

            model.Validate(out var errors).Should().BeTrue();
            errors.Should().BeEmpty();

            var universe = model.FindDeclaredType("Microsoft.Restier.Tests.Shared.Scenarios.Library.Universe")
             as IEdmComplexType;
            universe.Should().NotBeNull();

            var propertyArray = universe.Properties().ToArray();
            var i = 0;
            propertyArray[i++].Type.AsPrimitive().IsBinary().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsBoolean().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsByte().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsDateTimeOffset().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsDecimal().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsDouble().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsDuration().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsGuid().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsInt16().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsInt32().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsInt64().Should().BeTrue();

            // propertyArray[i++].Type.AsPrimitive().IsSByte().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsSingle().Should().BeTrue();

            // propertyArray[i++].Type.AsPrimitive().IsStream().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsString().Should().BeTrue();

            // propertyArray[i].Type.AsPrimitive().IsTimeOfDay().Should().BeTrue();
        }
    }
}
