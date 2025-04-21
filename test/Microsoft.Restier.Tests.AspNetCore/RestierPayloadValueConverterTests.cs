// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using FluentAssertions;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore;
using NSubstitute;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore;

/// <summary>
/// Unit tests for the <see cref="RestierPayloadValueConverter"/> class.
/// </summary>
public class RestierPayloadValueConverterTests
{
    private readonly RestierPayloadValueConverter _converter;

    public RestierPayloadValueConverterTests()
    {
        _converter = new RestierPayloadValueConverter();
    }

    [Fact]
    public void ConvertToPayloadValue_ShouldReturnDate_ForDateTimeAndEdmDate()
    {
        // Arrange
        var dateTime = new DateTime(2025, 4, 21);
        var edmTypeReference = EdmCoreModel.Instance.GetDate(false);

        // Act
        var result = _converter.ConvertToPayloadValue(dateTime, edmTypeReference);

        // Assert
        result.Should().BeOfType<Date>().Which.Should().BeEquivalentTo(new Date(2025, 4, 21));
    }

    [Fact]
    public void ConvertToPayloadValue_ShouldReturnDateTimeOffsetWithLocalOffset_ForDateTimeWithLocalKind()
    {
        // Arrange
        var dateTime = new DateTime(2025, 4, 21, 10, 0, 0, DateTimeKind.Local);
        var edmTypeReference = EdmCoreModel.Instance.GetDateTimeOffset(false);

        // Act
        var result = _converter.ConvertToPayloadValue(dateTime, edmTypeReference);

        // Assert
        result.Should().BeOfType<DateTimeOffset>().Which.Offset.Should().Be(TimeZoneInfo.Local.GetUtcOffset(dateTime));
    }

    [Fact]
    public void ConvertToPayloadValue_ShouldReturnDateTimeOffsetWithZeroOffset_ForDateTimeWithUtcKind()
    {
        // Arrange
        var dateTime = new DateTime(2025, 4, 21, 10, 0, 0, DateTimeKind.Utc);
        var edmTypeReference = EdmCoreModel.Instance.GetDateTimeOffset(false);

        // Act
        var result = _converter.ConvertToPayloadValue(dateTime, edmTypeReference);

        // Assert
        result.Should().BeOfType<DateTimeOffset>().Which.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ConvertToPayloadValue_ShouldReturnTimeOfDay_ForTimeSpanAndEdmTimeOfDay()
    {
        // Arrange
        var timeSpan = new TimeSpan(10, 30, 0);
        var edmTypeReference = EdmCoreModel.Instance.GetTimeOfDay(false);

        // Act
        var result = _converter.ConvertToPayloadValue(timeSpan, edmTypeReference);

        // Assert
        result.Should().BeOfType<TimeOfDay>().Which.Should().BeEquivalentTo(new TimeOfDay(10, 30, 0, 0));
    }

    [Fact]
    public void ConvertToPayloadValue_ShouldReturnDate_ForDateTimeOffsetAndEdmDate()
    {
        // Arrange
        var dateTimeOffset = new DateTimeOffset(2025, 4, 21, 10, 0, 0, TimeSpan.Zero);
        var edmTypeReference = EdmCoreModel.Instance.GetDate(false);

        // Act
        var result = _converter.ConvertToPayloadValue(dateTimeOffset, edmTypeReference);

        // Assert
        result.Should().BeOfType<Date>().Which.Should().BeEquivalentTo(new Date(2025, 4, 21));
    }

    [Fact]
    public void ConvertToPayloadValue_ShouldCallBaseMethod_ForUnsupportedTypes()
    {
        // Arrange
        var unsupportedValue = "unsupported";
        var edmTypeReference = Substitute.For<IEdmTypeReference>();

        var baseConverter = Substitute.For<ODataPayloadValueConverter>();
        var converter = Substitute.ForPartsOf<RestierPayloadValueConverter>();
        converter.When(x => x.ConvertToPayloadValue(unsupportedValue, edmTypeReference))
                 .DoNotCallBase();

        // Act
        converter.ConvertToPayloadValue(unsupportedValue, edmTypeReference);

        // Assert
        converter.Received(1).ConvertToPayloadValue(unsupportedValue, edmTypeReference);
    }
}
