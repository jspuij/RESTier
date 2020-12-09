// <copyright file="DataModificationItem.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Submit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Net;

    /// <summary>
    /// Represents a data modification item in a change set.
    /// </summary>
    public class DataModificationItem : ChangeSetItem
    {
        private const string IfNoneMatchKey = "@IfNoneMatchKey";

        /// <summary>
        /// Initializes a new instance of the <see cref="DataModificationItem" /> class.
        /// </summary>
        /// <param name="resourceSetName">
        /// The name of the resource set in question.
        /// </param>
        /// <param name="expectedResourceType">
        /// The type of the expected resource type in question.
        /// </param>
        /// <param name="actualResourceType">
        /// The type of the actual resource type in question.
        /// </param>
        /// <param name="action">
        /// The RestierEntitySetOperations for the request.
        /// </param>
        /// <param name="resourceKey">
        /// The key of the resource being modified.
        /// </param>
        /// <param name="originalValues">
        /// Any original values of the resource that are known.
        /// </param>
        /// <param name="localValues">
        /// The local values of the resource.
        /// </param>
        public DataModificationItem(
            string resourceSetName,
            Type expectedResourceType,
            Type actualResourceType,
            RestierEntitySetOperation action,
            IReadOnlyDictionary<string, object> resourceKey,
            IReadOnlyDictionary<string, object> originalValues,
            IReadOnlyDictionary<string, object> localValues)
            : base(ChangeSetItemType.DataModification)
        {
            Ensure.NotNull(resourceSetName, nameof(resourceSetName));
            Ensure.NotNull(expectedResourceType, nameof(expectedResourceType));
            this.ResourceSetName = resourceSetName;
            this.ExpectedResourceType = expectedResourceType;
            this.ActualResourceType = actualResourceType;
            this.ResourceKey = resourceKey;
            this.OriginalValues = originalValues;
            this.LocalValues = localValues;
            this.EntitySetOperation = action;
        }

        /// <summary>
        /// Gets the name of the resource set in question.
        /// </summary>
        public string ResourceSetName { get; private set; }

        /// <summary>
        /// Gets the name of the expected resource type in question.
        /// </summary>
        public Type ExpectedResourceType { get; private set; }

        /// <summary>
        /// Gets the name of the actual resource type in question.
        /// In type inheritance case, this is different from expectedResourceType.
        /// </summary>
        public Type ActualResourceType { get; private set; }

        /// <summary>
        /// Gets the key of the resource being modified.
        /// </summary>
        public IReadOnlyDictionary<string, object> ResourceKey { get; private set; }

        /// <summary>
        /// Gets or sets the action to be taken.
        /// </summary>
        public RestierEntitySetOperation EntitySetOperation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the resource should be fully replaced by the modification.
        /// </summary>
        /// <remarks>
        /// If true, all properties will be updated, even if the property isn't in LocalValues.
        /// If false, only properties identified in LocalValues will be updated on the resource.
        /// </remarks>
        public bool IsFullReplaceUpdateRequest { get; set; }

        /// <summary>
        /// Gets or sets the resource object in question.
        /// </summary>
        /// <remarks>
        /// Initially this will be <c>null</c>, however after the change
        /// set has been prepared it will represent the pending resource.
        /// </remarks>
        public object Resource { get; set; }

        /// <summary>
        /// Gets the original values for properties that have changed.
        /// </summary>
        /// <remarks>
        /// For new entities, this property is <c>null</c>.
        /// </remarks>
        public IReadOnlyDictionary<string, object> OriginalValues { get; private set; }

        /// <summary>
        /// Gets the local values for properties that have changed.
        /// </summary>
        /// <remarks>
        /// For entities pending deletion, this property is <c>null</c>.
        /// </remarks>
        public IReadOnlyDictionary<string, object> LocalValues { get; private set; }

        /// <summary>
        /// Applies the current DataModificationItem's KeyValues and OriginalValues to the
        /// specified query and returns the new query.
        /// </summary>
        /// <param name="query">The IQueryable to apply the property values to.</param>
        /// <returns>
        /// The new IQueryable with the property values applied to it in a Where condition.
        /// </returns>
        public IQueryable ApplyTo(IQueryable query)
        {
            Ensure.NotNull(query, nameof(query));
            if (this.EntitySetOperation == RestierEntitySetOperation.Insert)
            {
                throw new InvalidOperationException(Resources.DataModificationNotSupportCreateResource);
            }

            var type = query.ElementType;
            var param = Expression.Parameter(type);
            Expression where = null;

            if (this.ResourceKey != null)
            {
                foreach (var item in this.ResourceKey)
                {
                    where = ApplyPredicate(param, where, item);
                }
            }

            if (where == null)
            {
                throw new InvalidOperationException(Resources.DataModificationRequiresResourceKey);
            }

            var whereLambda = Expression.Lambda(where, param);
            return ExpressionHelpers.Where(query, whereLambda, type);
        }

        /// <summary>
        /// Validate the e-tag via applies the current DataModificationItem's OriginalValues to the
        /// specified query and returns result.
        /// </summary>
        /// <param name="query">The IQueryable to apply the property values to.</param>
        /// <returns>
        /// The object is e-tag checked passed.
        /// </returns>
        public object ValidateEtag(IQueryable query)
        {
            Ensure.NotNull(query, nameof(query));
            var type = query.ElementType;
            var param = Expression.Parameter(type);
            Expression where = null;

            if (this.OriginalValues != null)
            {
                foreach (var item in this.OriginalValues)
                {
                    if (!item.Key.StartsWith("@", StringComparison.Ordinal))
                    {
                        where = ApplyPredicate(param, where, item);
                    }
                }

                if (this.OriginalValues.ContainsKey(IfNoneMatchKey))
                {
                    where = Expression.Not(where);
                }
            }

            var whereLambda = Expression.Lambda(where, param);
            var queryable = ExpressionHelpers.Where(query, whereLambda, type);

            var matchedResource = queryable.SingleOrDefault();
            if (matchedResource == null)
            {
                // If ETAG does not match, should return 412 Precondition Failed
                var message = string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.PreconditionCheckFailed,
                    new object[] { this.EntitySetOperation, query.SingleOrDefault() });
                throw new StatusCodeException(HttpStatusCode.PreconditionFailed, message);
            }

            return matchedResource;
        }

        private static Expression ApplyPredicate(ParameterExpression param, Expression where, KeyValuePair<string, object> item)
        {
            var property = Expression.Property(param, item.Key);
            var itemValue = item.Value;

            if (itemValue.GetType() != property.Type)
            {
                itemValue = TypeConverter.ChangeType(itemValue, property.Type, CultureInfo.InvariantCulture);
            }

            // TODO GitHubIssue#31 : Check if LinqParameterContainer is necessary
            // Expression value = itemValue != null
            //     ? LinqParameterContainer.Parameterize(itemValue.GetType(), itemValue)
            //     : Expression.Constant(value: null);
            Expression left = property;
            Expression right = Expression.Constant(itemValue, property.Type);
            if (property.Type == typeof(byte[]))
            {
                left = Expression.Call(typeof(BitConverter), "ToString", null, new Expression[] { property });
                right = Expression.Call(typeof(BitConverter), "ToString", null, new Expression[] { Expression.Constant(itemValue, property.Type) });
            }

            var equal = Expression.Equal(left, right);
            return where == null ? equal : Expression.AndAlso(where, equal);
        }
    }
}
