// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Core.Model;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Restier.Core
{

    /// <summary>
    /// Extension methods to return IQueryable sources from an <see cref="ApiBase">.
    /// </summary>
    public static class QueryableApiExtensions
    {
        private static readonly MethodInfo SourceCoreMethod = typeof(QueryableApiExtensions)
            .GetMember("SourceCore", BindingFlags.NonPublic | BindingFlags.Static)
            .Cast<MethodInfo>()
            .Single(m => m.IsGenericMethod);

        private static readonly MethodInfo Source2Method = typeof(DataSourceStub)
            .GetMember("GetQueryableSource")
            .Cast<MethodInfo>()
            .Single(m => m.GetParameters().Length == 2);

        private static readonly MethodInfo Source3Method = typeof(DataSourceStub)
            .GetMember("GetQueryableSource")
            .Cast<MethodInfo>()
            .Single(m => m.GetParameters().Length == 3);

        #region GetQueryableSource

        /// <summary>
        /// Gets a queryable source of data using an API context.
        /// </summary>
        /// <param name="api">
        /// An API.
        /// </param>
        /// <param name="name">
        /// The name of an entity set, singleton or composable function import.
        /// </param>
        /// <param name="arguments">
        /// If <paramref name="name"/> is a composable function import,
        /// the arguments to be passed to the composable function import.
        /// </param>
        /// <returns>
        /// A queryable source.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If the name identifies a singleton or a composable function import
        /// whose result is a singleton, the resulting queryable source will
        /// be configured such that it represents exactly zero or one result.
        /// </para>
        /// <para>
        /// Note that the resulting queryable source cannot be synchronously
        /// enumerated as the API engine only operates asynchronously.
        /// </para>
        /// </remarks>
        public static IQueryable GetQueryableSource(this ApiBase api, string name, params object[] arguments)
        {
            Ensure.NotNull(api, nameof(api));
            Ensure.NotNull(name, nameof(name));

            return api.SourceCore(null, name, arguments);
        }

        /// <summary>
        /// Gets a queryable source of data using an API context.
        /// </summary>
        /// <param name="api">
        /// An API.
        /// </param>
        /// <param name="namespaceName">
        /// The name of a namespace containing a composable function.
        /// </param>
        /// <param name="name">
        /// The name of a composable function.
        /// </param>
        /// <param name="arguments">
        /// The arguments to be passed to the composable function.
        /// </param>
        /// <returns>
        /// A queryable source.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If the name identifies a composable function whose result is a
        /// singleton, the resulting queryable source will be configured such
        /// that it represents exactly zero or one result.
        /// </para>
        /// <para>
        /// Note that the resulting queryable source cannot be synchronously
        /// enumerated, as the API engine only operates asynchronously.
        /// </para>
        /// </remarks>
        public static IQueryable GetQueryableSource(this ApiBase api, string namespaceName, string name, params object[] arguments)
        {
            Ensure.NotNull(api, nameof(api));
            Ensure.NotNull(namespaceName, nameof(namespaceName));
            Ensure.NotNull(name, nameof(name));

            return SourceCore(api, namespaceName, name, arguments);
        }

        /// <summary>
        /// Gets a queryable source of data using an API context.
        /// </summary>
        /// <typeparam name="TElement">
        /// The type of the elements in the queryable source.
        /// </typeparam>
        /// <param name="api">
        /// An API.
        /// </param>
        /// <param name="name">
        /// The name of an entity set, singleton or composable function import.
        /// </param>
        /// <param name="arguments">
        /// If <paramref name="name"/> is a composable function import,
        /// the arguments to be passed to the composable function import.
        /// </param>
        /// <returns>
        /// A queryable source.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If the name identifies a singleton or a composable function import
        /// whose result is a singleton, the resulting queryable source will
        /// be configured such that it represents exactly zero or one result.
        /// </para>
        /// <para>
        /// Note that the resulting queryable source cannot be synchronously
        /// enumerated, as the API engine only operates asynchronously.
        /// </para>
        /// </remarks>
        public static IQueryable<TElement> GetQueryableSource<TElement>(this ApiBase api, string name, params object[] arguments)
        {
            Ensure.NotNull(api, nameof(api));
            Ensure.NotNull(name, nameof(name));

            var elementType = api.EnsureElementType(null, name);
            if (typeof(TElement) != elementType)
            {
                throw new ArgumentException(Resources.ElementTypeNotMatch);
            }

            return SourceCore<TElement>(null, name, arguments);
        }

        /// <summary>
        /// Gets a queryable source of data using an API context.
        /// </summary>
        /// <typeparam name="TElement">
        /// The type of the elements in the queryable source.
        /// </typeparam>
        /// <param name="api">
        /// An API.
        /// </param>
        /// <param name="namespaceName">
        /// The name of a namespace containing a composable function.
        /// </param>
        /// <param name="name">
        /// The name of a composable function.
        /// </param>
        /// <param name="arguments">
        /// The arguments to be passed to the composable function.
        /// </param>
        /// <returns>
        /// A queryable source.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If the name identifies a composable function whose result is a
        /// singleton, the resulting queryable source will be configured such
        /// that it represents exactly zero or one result.
        /// </para>
        /// <para>
        /// Note that the resulting queryable source cannot be synchronously
        /// enumerated, as the API engine only operates asynchronously.
        /// </para>
        /// </remarks>
        public static IQueryable<TElement> GetQueryableSource<TElement>(this ApiBase api, string namespaceName, string name, params object[] arguments)
        {
            Ensure.NotNull(api, nameof(api));
            Ensure.NotNull(namespaceName, nameof(namespaceName));
            Ensure.NotNull(name, nameof(name));

            var elementType = api.EnsureElementType(namespaceName, name);
            if (typeof(TElement) != elementType)
            {
                throw new ArgumentException(Resources.ElementTypeNotMatch);
            }

            return SourceCore<TElement>(namespaceName, name, arguments);
        }

        #endregion



        #region GetQueryableSource Private

        private static IQueryable SourceCore(this ApiBase api, string namespaceName, string name, object[] arguments)
        {
            var elementType = api.EnsureElementType(namespaceName, name);
            var method = SourceCoreMethod.MakeGenericMethod(elementType);
            var args = new object[] { namespaceName, name, arguments };
            return method.Invoke(null, args) as IQueryable;
        }

        private static IQueryable<TElement> SourceCore<TElement>(string namespaceName, string name, object[] arguments)
        {
            MethodInfo sourceMethod;
            Expression[] expressions;
            if (namespaceName is null)
            {
                sourceMethod = Source2Method;
                expressions = new Expression[]
                {
                    Expression.Constant(name),
                    Expression.Constant(arguments, typeof(object[]))
                };
            }
            else
            {
                sourceMethod = Source3Method;
                expressions = new Expression[]
                {
                    Expression.Constant(namespaceName),
                    Expression.Constant(name),
                    Expression.Constant(arguments, typeof(object[]))
                };
            }

            return new QueryableSource<TElement>(Expression.Call(null, sourceMethod.MakeGenericMethod(typeof(TElement)), expressions));
        }

        private static Type EnsureElementType(this ApiBase api, string namespaceName, string name)
        {
            var modelContext = new ModelContext(api);
            return api.QueryHandler.EnsureElementType(modelContext, namespaceName, name);
        }

        #endregion
    }

}
