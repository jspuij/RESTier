// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Interface that represents a bag of properties.
    /// </summary>
    public interface IPropertyBag
    {
        /// <summary>
        /// Indicates if this object has a property.
        /// </summary>
        /// <param name="name">The name of a property.</param>
        /// <returns>
        /// <c>true</c> if this object has the
        /// property; otherwise, <c>false</c>.
        /// </returns>
        bool HasProperty(string name);

        /// <summary>
        /// Gets a property.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the property.
        /// </typeparam>
        /// <param name="name">
        /// The name of a property.
        /// </param>
        /// <returns>
        /// The value of the property.
        /// </returns>
        T GetProperty<T>(string name);

        /// <summary>
        /// Gets a property.
        /// </summary>
        /// <param name="name">The name of a property.</param>
        /// <returns>The value of the property.</returns>
        object GetProperty(string name);

        /// <summary>
        /// Sets a property.
        /// </summary>
        /// <param name="name">
        /// The name of a property.
        /// </param>
        /// <param name="value">
        /// A value for the property.
        /// </param>
        void SetProperty(string name, object value);

        /// <summary>
        /// Removes a property.
        /// </summary>
        /// <param name="name">
        /// The name of a property.
        /// </param>
        void RemoveProperty(string name);
    }
}
