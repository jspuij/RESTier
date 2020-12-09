// <copyright file="RestierConventionDefinition.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.ConventionDefinitions
{
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.Restier.Core;

    /// <summary>
    /// A definition of a Restier convention.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public abstract class RestierConventionDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestierConventionDefinition"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="pipelineState">The pipeline state.</param>
        internal RestierConventionDefinition(string name, RestierPipelineState pipelineState)
        {
            this.Name = name;
            this.PipelineState = pipelineState;
        }

        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Restier Pipeline State.
        /// </summary>
        public RestierPipelineState? PipelineState { get; set; }
    }
}