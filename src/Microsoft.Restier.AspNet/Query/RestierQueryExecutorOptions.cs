// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.AspNet.Query
{
    internal class RestierQueryExecutorOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the total
        /// number of items should be retrieved when the
        /// result has been filtered using paging operators.
        /// </summary>
        /// <remarks>
        /// Setting this to <c>true</c> may have a performance impact as
        /// the data provider may need to execute two independent queries.
        /// </remarks>
        public bool IncludeTotalCount { get; set; }

        public Action<long> SetTotalCount { get; set; }
    }

    internal static class RestierQueryExecutorOptionsPropertyBagExtensions
    {
        internal static RestierQueryExecutorOptions GetRestierQueryExecutorOptions(this IPropertyBag propertyBag)
        {
            Ensure.NotNull(propertyBag, nameof(propertyBag));
            return propertyBag.GetProperty<RestierQueryExecutorOptions>(typeof(RestierQueryExecutorOptions).AssemblyQualifiedName);
        }

        internal static void SetRestierQueryExecutorOptions(this IPropertyBag propertyBag, RestierQueryExecutorOptions options)
        {
            Ensure.NotNull(propertyBag, nameof(propertyBag));
            Ensure.NotNull(options, nameof(options));
            propertyBag.SetProperty(typeof(RestierQueryExecutorOptions).AssemblyQualifiedName, options);
        }
    }
}
