// <copyright file="StatusCodeException.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;

    /// <summary>
    /// This exception is used for 404 Not found response.
    /// </summary>
    [Serializable]
    [ExcludeFromCodeCoverage]
    public class StatusCodeException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StatusCodeException"/> class.
        /// </summary>
        public StatusCodeException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusCodeException"/> class.
        /// </summary>
        /// <param name="message">Plain text error message for this exception.</param>
        public StatusCodeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusCodeException"/> class.
        /// </summary>
        /// <param name="message">Plain text error message for this exception.</param>
        /// <param name="innerException">Exception that caused this exception to be thrown.</param>
        public StatusCodeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusCodeException"/> class.
        /// </summary>
        /// <param name="statusCode">the <see cref="HttpStatusCode"/>.</param>
        /// <param name="message">Plain text error message for this exception.</param>
        public StatusCodeException(HttpStatusCode statusCode, string message)
            : this(statusCode, message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusCodeException"/> class.
        /// </summary>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/>.</param>
        /// <param name="message">Plain text error message for this exception.</param>
        /// <param name="innerException">Exception that caused this exception to be thrown.</param>
        public StatusCodeException(HttpStatusCode statusCode, string message, Exception innerException)
            : base(message, innerException)
        {
            this.StatusCode = statusCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusCodeException"/> class.
        /// </summary>
        /// <param name="serializationInfo">The serialization info.</param>
        /// <param name="streamingContext">The streaming context.</param>
        protected StatusCodeException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the <see cref="HttpStatusCode"/>.
        /// </summary>
        public HttpStatusCode StatusCode { get; private set; } = HttpStatusCode.BadRequest;
    }
}