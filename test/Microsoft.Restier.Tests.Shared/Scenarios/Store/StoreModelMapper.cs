// <copyright file="StoreModelMapper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.Restier.Core.Model;

    /// <summary>
    /// Store model mapper.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class StoreModelMapper : IModelMapper
    {
        /// <inheritdoc />
        public bool TryGetRelevantType(ModelContext context, string name, out Type relevantType)
        {
            if (name == "Products")
            {
                relevantType = typeof(Product);
            }
            else if (name == "Customers")
            {
                relevantType = typeof(Customer);
            }
            else if (name == "Stores")
            {
                relevantType = typeof(Store);
            }
            else
            {
                relevantType = null;
            }

            return true;
        }

        /// <inheritdoc />
        public bool TryGetRelevantType(ModelContext context, string namespaceName, string name, out Type relevantType)
        {
            relevantType = typeof(Product);
            return true;
        }
    }
}
