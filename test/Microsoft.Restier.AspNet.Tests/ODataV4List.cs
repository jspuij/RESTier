// <copyright file="ODataV4List.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.AspNet.Tests
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// OData V4 list.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    public class ODataV4List<T>
    {
        /// <summary>
        /// Gets or sets the OData context.
        /// </summary>
        [JsonProperty("@odata.context")]
        public string ODataContext { get; set; }

        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        [JsonProperty("value")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<T> Items { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}