// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Restier.Core.Model
{
    /// <summary>
    /// Represents a context for either model building or request models.
    /// </summary>
    public interface IModelContext
    {
        /// <summary>
        /// Gets resource set and resource type map dictionary.
        /// </summary>
        public IDictionary<string, Type> ResourceSetTypeMap { get; }

        /// <summary>
        /// Gets resource type and its key properties map dictionary.
        /// This is useful when key properties does not have key attribute
        /// or follow Web Api OData key property naming convention.
        /// Otherwise, this collection is not needed.
        /// </summary>
        public IDictionary<Type, ICollection<PropertyInfo>> ResourceTypeKeyPropertiesMap { get; }
    }
}