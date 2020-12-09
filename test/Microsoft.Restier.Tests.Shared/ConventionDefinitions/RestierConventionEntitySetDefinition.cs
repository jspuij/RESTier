// <copyright file="RestierConventionEntitySetDefinition.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.ConventionDefinitions
{
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.Restier.Core;

    /// <summary>
    /// Definition for restier convention regarding EntitySets.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class RestierConventionEntitySetDefinition : RestierConventionDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestierConventionEntitySetDefinition"/> class.
        /// Creates a new <see cref="RestierConventionEntitySetDefinition"/> instance.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="pipelineState">The pipeline state.</param>
        /// <param name="entitySetName">The name of the entity set.</param>
        /// <param name="entitySetOperation">The entity set operation.</param>
        internal RestierConventionEntitySetDefinition(string name, RestierPipelineState pipelineState, string entitySetName, RestierEntitySetOperation entitySetOperation)
            : base(name, pipelineState)
        {
            this.EntitySetName = entitySetName;
            this.EntitySetOperation = entitySetOperation;
        }

        /// <summary>
        /// Gets or sets the name of the EntitySet associated with this ConventionDefinition.
        /// </summary>
        public string EntitySetName { get; set; }

        /// <summary>
        /// Gets or sets the Restier Operation associated with this ConventionDefinition.
        /// </summary>
        public RestierEntitySetOperation EntitySetOperation { get; set; }
    }
}