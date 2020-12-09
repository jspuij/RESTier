// <copyright file="QueryableSource{T}.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Represents a typed <see cref="QueryableSource"/>.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    internal class QueryableSource<T> : QueryableSource, IOrderedQueryable<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryableSource{T}"/> class.
        /// </summary>
        /// <param name="expression">The query expression.</param>
        public QueryableSource(Expression expression)
            : base(expression)
        {
        }

        /// <inheritdoc />
        public override Type ElementType => typeof(T);

        /// <inheritdoc />
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotSupportedException(Resources.CallQueryableSourceMethodNotSupported);
    }
}
