// <copyright file="JsonTimeSpanConverter.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.Common
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Xml;
    using Newtonsoft.Json;

    /// <summary>
    /// A timespan converter for JSon.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class JsonTimeSpanConverter : JsonConverter
    {
        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanWrite => true;

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TimeSpan);
        }

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType != typeof(TimeSpan))
            {
                throw new ArgumentException("Object passed in was not a TimeSpan.", nameof(objectType));
            }

            if (!(reader.Value is string spanString))
            {
                return null;
            }

#pragma warning disable CA1307 // Specify StringComparison
            if (spanString.Contains("-"))
            {
                spanString = $"-{spanString.Replace("-", string.Empty)}";
#pragma warning restore CA1307 // Specify StringComparison
            }

            return XmlConvert.ToTimeSpan(spanString);
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var duration = (TimeSpan)value;
            writer.WriteValue(XmlConvert.ToString(duration));
        }
    }
}