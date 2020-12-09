// <copyright file="ChangeSetValidationException.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Submit
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    /// <summary>
    /// Represents an exception that indicates validation errors occurred on entities.
    /// </summary>
    [Serializable]
    [ExcludeFromCodeCoverage]
    public class ChangeSetValidationException : Exception
    {
        private IEnumerable<ChangeSetItemValidationResult> errorValidationResults;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeSetValidationException"/> class.
        /// </summary>
        public ChangeSetValidationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeSetValidationException"/> class.
        /// </summary>
        /// <param name="message">Message of the exception.</param>
        public ChangeSetValidationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeSetValidationException"/> class.
        /// </summary>
        /// <param name="message">Message of the exception.</param>
        /// <param name="innerException">Inner exception.</param>
        public ChangeSetValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeSetValidationException"/> class.
        /// </summary>
        /// <param name="serializationInfo">The serialization info.</param>
        /// <param name="streamingContext">The streaming context.</param>
        protected ChangeSetValidationException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets or sets the failed validation results.
        /// </summary>
        public IEnumerable<ChangeSetItemValidationResult> ValidationResults
        {
            get => this.errorValidationResults ?? Enumerable.Empty<ChangeSetItemValidationResult>();
            set => this.errorValidationResults = value;
        }
    }
}