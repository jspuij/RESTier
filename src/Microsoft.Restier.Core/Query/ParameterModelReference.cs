// <copyright file="ParameterModelReference.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Query
{
    using Microsoft.OData.Edm;

    /// <summary>
    /// Represents a reference to parameter data in terms of a model.
    /// It does not have special logic.
    /// </summary>
    public class ParameterModelReference : QueryModelReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterModelReference"/> class.
        /// </summary>
        /// <param name="entitySet">The EntitySet to reference.</param>
        /// <param name="type">The item Type.</param>
        internal ParameterModelReference(IEdmEntitySet entitySet, IEdmType type)
            : base(entitySet, type)
        {
        }
    }
}
