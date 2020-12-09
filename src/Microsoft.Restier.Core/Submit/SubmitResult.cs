// <copyright file="SubmitResult.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Submit
{
    using System;

    /// <summary>
    /// Represents a submit result.
    /// </summary>
    public class SubmitResult
    {
        private Exception exception;
        private ChangeSet completedChangeSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubmitResult" /> class with an error.
        /// </summary>
        /// <param name="exception">
        /// An error.
        /// </param>
        public SubmitResult(Exception exception)
        {
            Ensure.NotNull(exception, nameof(exception));
            this.Exception = exception;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubmitResult" /> class.
        /// </summary>
        /// <param name="completedChangeSet">
        /// A completed change set.
        /// </param>
        public SubmitResult(ChangeSet completedChangeSet)
        {
            Ensure.NotNull(completedChangeSet, nameof(completedChangeSet));
            this.completedChangeSet = completedChangeSet;
        }

        /// <summary>
        /// Gets or sets an error to be returned.
        /// </summary>
        /// <remarks>
        /// Setting this value will override any
        /// existing error or completed change set.
        /// </remarks>
        public Exception Exception
        {
            get => this.exception;

            set
            {
                Ensure.NotNull(value, nameof(value));
                this.exception = value;
                this.completedChangeSet = null;
            }
        }

        /// <summary>
        /// Gets or sets the completed change set.
        /// </summary>
        /// <remarks>
        /// Setting this value will override any
        /// existing error or completed change set.
        /// </remarks>
        public ChangeSet CompletedChangeSet
        {
            get => this.completedChangeSet;

            set
            {
                Ensure.NotNull(value, nameof(value));
                this.exception = null;
                this.completedChangeSet = value;
            }
        }
    }
}
