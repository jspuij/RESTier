// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents context under which an request is processed.
    /// The request could be a query, a submit, an operation execution or a model retrieve.
    /// It has subclass for each kinds of request.
    /// </summary>
    /// <remarks>
    /// An invocation context is created each time an request is parsed to a specified request.
    /// </remarks>
    public class InvocationContext : IPropertyBag
    {
        private readonly IPropertyBag propertyBag;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvocationContext" /> class.
        /// </summary>
        /// <param name="api">
        /// An Api.
        /// </param>
        /// <param name="propertyBag">
        /// An <see cref="IPropertyBag"/> implementation to hold state in this context.
        /// </param>
        public InvocationContext(ApiBase api, IPropertyBag propertyBag)
        {
            Ensure.NotNull(api, nameof(api));
            Ensure.NotNull(propertyBag, nameof(propertyBag));

            this.propertyBag = propertyBag;
            Api = api;
        }

        /// <summary>
        /// Gets the <see cref="ApiBase"/> descendant for this invocation.
        /// </summary>
        public ApiBase Api { get; }

        #region IPropertyBag Forwarding

        /// <inheritdoc />
        public T GetProperty<T>(string name)
        {
            return propertyBag.GetProperty<T>(name);
        }

        /// <inheritdoc />
        public object GetProperty(string name)
        {
            return propertyBag.GetProperty(name);
        }

        /// <inheritdoc />
        public bool HasProperty(string name)
        {
            return propertyBag.HasProperty(name);
        }

        /// <inheritdoc />
        public void RemoveProperty(string name)
        {
            propertyBag.RemoveProperty(name);
        }

        /// <inheritdoc />
        public void SetProperty(string name, object value)
        {
            propertyBag.SetProperty(name, value);
        }

        #endregion
    }
}
