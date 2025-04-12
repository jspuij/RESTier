// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.OData.Formatter.Deserialization;
using Microsoft.OData.Edm;
using System;

namespace Microsoft.Restier.AspNetCore.Formatter
{

    /// <summary>
    /// The default deserializer provider.
    /// </summary>
    public class DefaultRestierDeserializerProvider : ODataDeserializerProvider
    {
        private readonly RestierEnumDeserializer enumDeserializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRestierDeserializerProvider" /> class.
        /// </summary>
        /// <param name="rootContainer">The container to get the service</param>
        public DefaultRestierDeserializerProvider(IServiceProvider rootContainer) : base(rootContainer) => enumDeserializer = new RestierEnumDeserializer();

        /// <inheritdoc />
        public override IODataEdmTypeDeserializer GetEdmTypeDeserializer(IEdmTypeReference edmType, bool isDelta = false)
        {
            if (edmType.IsEnum())
            {
                return enumDeserializer;
            }

            return base.GetEdmTypeDeserializer(edmType, isDelta);
        }

    }

}
