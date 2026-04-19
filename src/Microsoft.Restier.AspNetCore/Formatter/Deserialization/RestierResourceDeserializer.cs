// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.OData.Formatter.Deserialization;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.OData;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.AspNetCore.Formatter
{
    /// <summary>
    /// A custom OData resource deserializer that fixes property name mapping when
    /// <c>EnableLowerCamelCase</c> is active. The base <see cref="ODataResourceDeserializer"/>
    /// uses <c>ClrPropertyInfoAnnotation</c> to resolve CLR property names, then passes those
    /// PascalCase names to <see cref="EdmStructuredObject.TrySetPropertyValue"/>.
    /// But <c>EdmStructuredObject</c> validates property names against the EDM type, which has
    /// camelCase names — causing a silent mismatch. This override uses the EDM property name instead.
    /// </summary>
    internal class RestierResourceDeserializer : ODataResourceDeserializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestierResourceDeserializer"/> class.
        /// </summary>
        /// <param name="deserializerProvider">The deserializer provider.</param>
        public RestierResourceDeserializer(IODataDeserializerProvider deserializerProvider)
            : base(deserializerProvider)
        {
        }

        /// <inheritdoc />
        public override void ApplyStructuralProperty(object resource, ODataProperty structuralProperty,
            IEdmStructuredTypeReference structuredType, ODataDeserializerContext readContext)
        {
            if (resource is EdmStructuredObject edmObject)
            {
                // For EdmStructuredObject, use the EDM property name (which may be camelCase)
                // instead of the CLR name that the base class resolves via ClrPropertyInfoAnnotation.
                var edmProperty = structuredType.FindProperty(structuralProperty.Name);
                if (edmProperty is not null)
                {
                    var value = structuralProperty.Value;

                    // Handle ODataUntypedValue and ODataEnumValue specially
                    if (value is ODataUntypedValue untypedValue)
                    {
                        edmObject.TrySetPropertyValue(structuralProperty.Name, untypedValue.RawValue);
                        return;
                    }

                    if (value is ODataEnumValue enumValue)
                    {
                        // Store as string, matching what RestierEnumDeserializer and
                        // EFChangeSetInitializer.ConvertToEfValue expect (Enum.Parse from string).
                        edmObject.TrySetPropertyValue(structuralProperty.Name, enumValue.Value);
                        return;
                    }

                    edmObject.TrySetPropertyValue(structuralProperty.Name, value);
                    return;
                }
            }

            // For CLR objects (not EdmStructuredObject), use the base implementation
            // which correctly maps EDM names to CLR property names via reflection.
            base.ApplyStructuralProperty(resource, structuralProperty, structuredType, readContext);
        }
    }
}
