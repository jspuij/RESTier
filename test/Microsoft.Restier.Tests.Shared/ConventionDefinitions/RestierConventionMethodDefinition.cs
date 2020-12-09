// <copyright file="RestierConventionMethodDefinition.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.ConventionDefinitions
{
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.Restier.Core;

    /// <summary>
    /// Restier convention definition for methods.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class RestierConventionMethodDefinition : RestierConventionDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestierConventionMethodDefinition"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="pipelineState">The pipeline state.</param>
        /// <param name="methodName">The method name.</param>
        /// <param name="methodOperation">The method operation.</param>
        public RestierConventionMethodDefinition(string name, RestierPipelineState pipelineState, string methodName, RestierOperationMethod methodOperation)
            : base(name, pipelineState)
        {
            this.MethodName = methodName;
            this.MethodOperation = methodOperation;
        }

        /// <summary>
        /// Gets or sets the method name.
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// Gets or sets the method operation.
        /// </summary>
        public RestierOperationMethod MethodOperation { get; set; }
    }
}