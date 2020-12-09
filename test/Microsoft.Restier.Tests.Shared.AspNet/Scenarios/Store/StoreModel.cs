// <copyright file="StoreModel.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.AspNet.Scenarios.Store
{
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.AspNet.OData.Builder;
    using Microsoft.OData.Edm;
    using Microsoft.Restier.Tests.Shared;

    /// <summary>
    /// Store model.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal static class StoreModel
    {
        static StoreModel()
        {
            var builder = new ODataConventionModelBuilder
            {
                Namespace = "Microsoft.Restier.Tests.Shared",
            };
            builder.EntitySet<Product>("Products");
            builder.EntitySet<Customer>("Customers");
            builder.EntitySet<Store>("Stores");
            builder.Function("GetBestProduct").ReturnsFromEntitySet<Product>("Products");
            builder.Action("RemoveWorstProduct").ReturnsFromEntitySet<Product>("Products");
            Model = (EdmModel)builder.GetEdmModel();
            Product = (IEdmEntityType)Model.FindType("Microsoft.Restier.Tests.Shared");
        }

        /// <summary>
        /// Gets the model.
        /// </summary>
        public static EdmModel Model { get; private set; }

        /// <summary>
        /// Gets the product.
        /// </summary>
        public static IEdmEntityType Product { get; private set; }
    }
}