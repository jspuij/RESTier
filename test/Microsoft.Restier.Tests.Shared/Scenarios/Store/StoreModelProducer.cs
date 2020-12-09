// <copyright file="StoreModelProducer.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.OData.Edm;
    using Microsoft.Restier.Core.Model;

    /// <summary>
    /// Store Test model producer.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class StoreModelProducer : IModelBuilder
    {
        private readonly EdmModel model;

        /// <summary>
        /// Initializes a new instance of the <see cref="StoreModelProducer"/> class.
        /// </summary>
        /// <param name="model">The model to use.</param>
        public StoreModelProducer(EdmModel model)
        {
            this.model = model;
        }

        /// <inheritdoc />
        public Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult<IEdmModel>(this.model);
        }
    }
}
