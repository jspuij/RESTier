// <copyright file="RestierRawSerializer.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

#if NETCOREAPP
namespace Microsoft.Restier.AspNetCore.Formatter
#else
namespace Microsoft.Restier.AspNet.Formatter
#endif
{
    using System;
    using Microsoft.AspNet.OData.Formatter.Serialization;
    using Microsoft.OData;

    /// <summary>
    /// The serializer for raw result.
    /// </summary>
    public class RestierRawSerializer : ODataRawValueSerializer
    {
        private readonly ODataPayloadValueConverter payloadValueConverter;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierRawSerializer"/> class.
        /// </summary>
        /// <param name="payloadValueConverter">The <see cref="ODataPayloadValueConverter"/> to use.</param>
        public RestierRawSerializer(ODataPayloadValueConverter payloadValueConverter)
        {
            Ensure.NotNull(payloadValueConverter, nameof(payloadValueConverter));
            this.payloadValueConverter = payloadValueConverter;
        }

        /// <summary>
        /// Writes the entity result to the response message.
        /// </summary>
        /// <param name="graph">The entity result to write.</param>
        /// <param name="type">The type of the entity.</param>
        /// <param name="messageWriter">The message writer.</param>
        /// <param name="writeContext">The writing context.</param>
        public override void WriteObject(
            object graph,
            Type type,
            ODataMessageWriter messageWriter,
            ODataSerializerContext writeContext)
        {
            RawResult rawResult = graph as RawResult;
            if (rawResult != null)
            {
                graph = rawResult.Result;
                type = rawResult.Type;
            }

            if (writeContext != null)
            {
                graph = RestierPrimitiveSerializer.ConvertToPayloadValue(graph, writeContext, this.payloadValueConverter);
            }

            if (graph == null)
            {
                // This is to make ODataRawValueSerializer happily serialize null value.
                // JWS: Is this correct?
                graph = string.Empty;
            }

            base.WriteObject(graph, type, messageWriter, writeContext);
        }
    }
}
