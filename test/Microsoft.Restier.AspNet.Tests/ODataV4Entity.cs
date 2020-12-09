// <copyright file="ODataV4Entity.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.AspNet.Tests
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Odata V4 Entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public class ODataV4Entity<T>
    {
        /// <summary>
        /// Gets or sets get or set the OData type.
        /// </summary>
        [JsonProperty("@odata.type")]
        public string ODataType { get; set; }

        /// <summary>
        /// Gets or sets get or set the OData Id.
        /// </summary>
        [JsonProperty("@odata.id")]
        public string ODataId { get; set; }

        /// <summary>
        /// Gets or sets the OData Edit link.
        /// </summary>
        [JsonProperty("@odata.editLink")]
        public string ODataEditLink { get; set; }

        /// <summary>
        /// Gets or sets the Odata Id Type.
        /// </summary>
        [JsonProperty("Id@odata.type")]
        public string ODataIdType { get; set; }

        /// <summary>
        /// Gets or sets the Items.
        /// </summary>
        [JsonProperty("value")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<T> Items { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}