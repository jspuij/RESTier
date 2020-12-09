// <copyright file="RestierQueryBuilder.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

#if NETCOREAPP
namespace Microsoft.Restier.AspNetCore.Query
#else
namespace Microsoft.Restier.AspNet.Query
#endif
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Microsoft.OData.Edm;
    using Microsoft.OData.UriParser;
#if NETCOREAPP
    using Microsoft.Restier.AspNetCore.Model;
    using AspNetResources = Microsoft.Restier.AspNetCore.Resources;
#else
    using Microsoft.Restier.AspNet.Model;
    using AspNetResources = Microsoft.Restier.AspNet.Resources;
#endif
    using Microsoft.Restier.Core;
    using ODataPath = Microsoft.AspNet.OData.Routing.ODataPath;

    /// <summary>
    /// Restier Query Builder. Builds a Linq Query based on the received path.
    /// </summary>
    internal class RestierQueryBuilder
    {
        private const string DefaultNameOfParameterExpression = "currentValue";

        private readonly ApiBase api;
        private readonly ODataPath path;
        private readonly IDictionary<Type, Action<ODataPathSegment>> handlers = new Dictionary<Type, Action<ODataPathSegment>>();
        private readonly IEdmModel edmModel;

        private IQueryable queryable;
        private Type currentType;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierQueryBuilder"/> class.
        /// </summary>
        /// <param name="api">The Api to use.</param>
        /// <param name="path">The path to process.</param>
        public RestierQueryBuilder(ApiBase api, ODataPath path)
        {
            Ensure.NotNull(api, nameof(api));
            Ensure.NotNull(path, nameof(path));
            this.api = api;
            this.path = path;

            // TODO: JWS: At best a hack to avoid a deadlock, because the only place to get the model is in a synchronous method or
            // constructor. See https://blog.stephencleary.com/2012/07/dont-block-on-async-code.html
            this.edmModel = Task.Run(() => this.api.GetModelAsync()).Result;

            this.handlers[typeof(EntitySetSegment)] = this.HandleEntitySetPathSegment;
            this.handlers[typeof(SingletonSegment)] = this.HandleSingletonPathSegment;
            this.handlers[typeof(OperationImportSegment)] = this.EmptyHandler;
            this.handlers[typeof(CountSegment)] = this.HandleCountPathSegment;
            this.handlers[typeof(ValueSegment)] = this.HandleValuePathSegment;
            this.handlers[typeof(KeySegment)] = this.HandleKeyValuePathSegment;
            this.handlers[typeof(NavigationPropertySegment)] = this.HandleNavigationPathSegment;
            this.handlers[typeof(PropertySegment)] = this.HandlePropertyAccessPathSegment;
            this.handlers[typeof(TypeSegment)] = this.HandleEntityTypeSegment;
            this.handlers[typeof(OperationSegment)] = this.EmptyHandler;

            // Complex cast is not supported by EF, and is not supported here
            // this.handlers[ODataSegmentKinds.ComplexCast] = null;
        }

        /// <summary>
        /// Gets a value indicating whether a Count path segment is present.
        /// </summary>
        public bool IsCountPathSegmentPresent { get; private set; }

        /// <summary>
        /// Gets a value indicating whether a value path segment is present.
        /// </summary>
        public bool IsValuePathSegmentPresent { get; private set; }

        /// <summary>
        /// Builds an <see cref="IQueryable"/> based on the path.
        /// </summary>
        /// <returns>An <see cref="IQueryable"/> instance.</returns>
        public IQueryable BuildQuery()
        {
            this.queryable = null;

            foreach (var segment in this.path.Segments)
            {
                if (!this.handlers.TryGetValue(segment.GetType(), out var handler))
                {
                    throw new NotImplementedException(
                        string.Format(CultureInfo.InvariantCulture, AspNetResources.PathSegmentNotSupported, segment));
                }

                handler(segment);
            }

            return this.queryable;
        }

        /// <summary>
        /// Get key values from the path.
        /// </summary>
        /// <param name="path">the path.</param>
        /// <returns>A dictionary with key names and values.</returns>
        internal static IReadOnlyDictionary<string, object> GetPathKeyValues(ODataPath path)
        {
            if (path.PathTemplate == "~/entityset/key" ||
                path.PathTemplate == "~/entityset/key/cast")
            {
                var keySegment = (KeySegment)path.Segments[1];
                return GetPathKeyValues(keySegment);
            }
            else if (path.PathTemplate == "~/entityset/cast/key")
            {
                var keySegment = (KeySegment)path.Segments[2];
                return GetPathKeyValues(keySegment);
            }
            else
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    AspNetResources.InvalidPathTemplateInRequest,
                    "~/entityset/key"));
            }
        }

        private static IReadOnlyDictionary<string, object> GetPathKeyValues(
            KeySegment keySegment)
        {
            var result = new Dictionary<string, object>();

            // TODO GitHubIssue#42 : Improve key parsing logic
            // this parsing implementation does not allow key values to contain commas
            // Depending on the WebAPI to make KeyValuePathSegment.Values collection public
            // (or have the parsing logic public).
            var keyValuePairs = keySegment.Keys;

            foreach (var keyValuePair in keyValuePairs)
            {
                result.Add(keyValuePair.Key, keyValuePair.Value);
            }

            return result;
        }

        private static BinaryExpression CreateEqualsExpression(
            ParameterExpression parameterExpression,
            string propertyName,
            object propertyValue)
        {
            var property = Expression.Property(parameterExpression, propertyName);
            var constant = Expression.Constant(
                TypeConverter.ChangeType(propertyValue, property.Type, CultureInfo.InvariantCulture));
            return Expression.Equal(property, constant);
        }

        private static LambdaExpression CreateNotEqualsNullExpression(
            Expression propertyExpression, ParameterExpression parameterExpression)
        {
            var nullConstant = Expression.Constant(null);
            var nullFilterExpression = Expression.NotEqual(propertyExpression, nullConstant);
            var whereExpression = Expression.Lambda(nullFilterExpression, parameterExpression);

            return whereExpression;
        }

        private void HandleEntitySetPathSegment(ODataPathSegment segment)
        {
            var entitySetPathSegment = (EntitySetSegment)segment;
            var entitySet = entitySetPathSegment.EntitySet;
            this.queryable = this.api.GetQueryableSource(entitySet.Name, (object[])null);
            this.currentType = this.queryable.ElementType;
        }

        private void HandleSingletonPathSegment(ODataPathSegment segment)
        {
            var singletonPathSegment = (SingletonSegment)segment;
            var singleton = singletonPathSegment.Singleton;
            this.queryable = this.api.GetQueryableSource(singleton.Name, (object[])null);
            this.currentType = this.queryable.ElementType;
        }

        private void EmptyHandler(ODataPathSegment segment)
        {
            // Nothing will be done
        }

        private void HandleCountPathSegment(ODataPathSegment segment) => this.IsCountPathSegmentPresent = true;

        private void HandleValuePathSegment(ODataPathSegment segment) => this.IsValuePathSegmentPresent = true;

        private void HandleKeyValuePathSegment(ODataPathSegment segment)
        {
            var keySegment = (KeySegment)segment;

            var parameterExpression = Expression.Parameter(this.currentType, DefaultNameOfParameterExpression);
            var keyValues = GetPathKeyValues(keySegment);

            BinaryExpression keyFilter = null;
            foreach (var keyValuePair in keyValues)
            {
                var equalsExpression =
                    CreateEqualsExpression(parameterExpression, keyValuePair.Key, keyValuePair.Value);
                keyFilter = keyFilter == null ? equalsExpression : Expression.And(keyFilter, equalsExpression);
            }

            var whereExpression = Expression.Lambda(keyFilter, parameterExpression);
            this.queryable = ExpressionHelpers.Where(this.queryable, whereExpression, this.currentType);
        }

        private void HandleNavigationPathSegment(ODataPathSegment segment)
        {
            var navigationSegment = (NavigationPropertySegment)segment;
            var entityParameterExpression = Expression.Parameter(this.currentType);
            var navigationPropertyExpression =
                Expression.Property(entityParameterExpression, navigationSegment.NavigationProperty.Name);

            if (navigationSegment.NavigationProperty.TargetMultiplicity() == EdmMultiplicity.Many)
            {
                // get the element type of the target
                // (the type should be an EntityCollection<T> for navigation queries).
                this.currentType = navigationPropertyExpression.Type.GetEnumerableItemType();

                // need to explicitly define the delegate type as IEnumerable<T>
                var delegateType = typeof(Func<,>).MakeGenericType(
                    this.queryable.ElementType,
                    typeof(IEnumerable<>).MakeGenericType(this.currentType));
                var selectBody =
                    Expression.Lambda(delegateType, navigationPropertyExpression, entityParameterExpression);

                this.queryable = ExpressionHelpers.SelectMany(this.queryable, selectBody, this.currentType);
            }
            else
            {
                // Check whether property is null or not before further selection
                // RWM: Removed from the outer loop because I don't believe it is necessary for Collection properties.
                var whereExpression = CreateNotEqualsNullExpression(navigationPropertyExpression, entityParameterExpression);
                this.queryable = ExpressionHelpers.Where(this.queryable, whereExpression, this.currentType);

                this.currentType = navigationPropertyExpression.Type;
                var selectBody =
                    Expression.Lambda(navigationPropertyExpression, entityParameterExpression);
                this.queryable = ExpressionHelpers.Select(this.queryable, selectBody);
            }
        }

        private void HandlePropertyAccessPathSegment(ODataPathSegment segment)
        {
            var propertySegment = (PropertySegment)segment;
            var entityParameterExpression = Expression.Parameter(this.currentType);
            var structuralPropertyExpression =
                Expression.Property(entityParameterExpression, propertySegment.Property.Name);

            // Check whether property is null or not before further selection
            if (propertySegment.Property.Type.IsNullable && !propertySegment.Property.Type.IsPrimitive())
            {
                var whereExpression =
                    CreateNotEqualsNullExpression(structuralPropertyExpression, entityParameterExpression);
                this.queryable = ExpressionHelpers.Where(this.queryable, whereExpression, this.currentType);
            }

            if (propertySegment.Property.Type.IsCollection())
            {
                // Produces new query like 'queryable.SelectMany(param => param.PropertyName)'.
                // Suppose 'param.PropertyName' is of type 'IEnumerable<T>', the type of the
                // resulting query would be 'IEnumerable<T>' too.
                this.currentType = structuralPropertyExpression.Type.GetEnumerableItemType();
                var delegateType = typeof(Func<,>).MakeGenericType(
                    this.queryable.ElementType,
                    typeof(IEnumerable<>).MakeGenericType(this.currentType));
                var selectBody =
                    Expression.Lambda(delegateType, structuralPropertyExpression, entityParameterExpression);
                this.queryable = ExpressionHelpers.SelectMany(this.queryable, selectBody, this.currentType);
            }
            else
            {
                // Produces new query like 'queryable.Select(param => param.PropertyName)'.
                this.currentType = structuralPropertyExpression.Type;
                var selectBody =
                    Expression.Lambda(structuralPropertyExpression, entityParameterExpression);
                this.queryable = ExpressionHelpers.Select(this.queryable, selectBody);
            }
        }

        // This only covers entity type cast
        // complex type cast uses ComplexCastPathSegment and is not supported by EF now
        // CLR type is got from model annotation, which means model must include that annotation.
        private void HandleEntityTypeSegment(ODataPathSegment segment)
        {
            var typeSegment = (TypeSegment)segment;
            var edmType = typeSegment.EdmType;

            if (typeSegment.EdmType.TypeKind == EdmTypeKind.Collection)
            {
                edmType = ((IEdmCollectionType)typeSegment.EdmType).ElementType.Definition;
            }

            if (edmType.TypeKind == EdmTypeKind.Entity)
            {
                this.currentType = edmType.GetClrType(this.edmModel);
                this.queryable = ExpressionHelpers.OfType(this.queryable, this.currentType);
            }
        }
    }
}
