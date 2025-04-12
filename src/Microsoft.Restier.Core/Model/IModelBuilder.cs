// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Core.Model
{
    /// <summary>
    /// The service for model generation.
    /// </summary>
    public interface IModelBuilder
    {
        /// <summary>
        /// Asynchronously gets an API model for an API.
        /// </summary>
        /// <returns>
        /// Constructs the Edm Model for the API.
        /// </returns>
        IEdmModel GetEdmModel(IModelContext modelContext);

    }

}
