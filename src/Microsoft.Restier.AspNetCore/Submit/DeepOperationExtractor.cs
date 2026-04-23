// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.AspNetCore.Submit
{
    /// <summary>
    /// Walks an EdmEntityObject and extracts nested entities into a DataModificationItem tree.
    /// Entity references (@odata.bind in 4.0, @id in 4.01) are stored as NavigationBindings on the parent.
    /// </summary>
    internal class DeepOperationExtractor
    {
        private readonly IEdmModel model;
        private readonly ApiBase api;
        private readonly DeepOperationSettings settings;

        public DeepOperationExtractor(IEdmModel model, ApiBase api, DeepOperationSettings settings)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.api = api ?? throw new ArgumentNullException(nameof(api));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void ExtractNestedItems(
            Delta entity,
            IEdmStructuredType edmType,
            DataModificationItem parentItem,
            bool isCreation,
            int currentDepth = 0)
        {
            if (settings.MaxDepth > 0 && currentDepth >= settings.MaxDepth)
            {
                throw new ODataException($"Deep operation exceeds maximum nesting depth of {settings.MaxDepth}.");
            }

            foreach (var propertyName in entity.GetChangedPropertyNames())
            {
                if (!entity.TryGetPropertyValue(propertyName, out var value) || value is null)
                {
                    continue;
                }

                var edmProperty = edmType.FindProperty(propertyName);
                if (edmProperty is not IEdmNavigationProperty navProperty)
                {
                    continue;
                }

                var clrPropertyName = EdmClrPropertyMapper.GetClrPropertyName(edmProperty, model);
                var targetEntityType = navProperty.ToEntityType();
                var targetEntitySet = FindTargetEntitySet(navProperty);

                if (value is EdmEntityObject nestedEntity)
                {
                    ProcessSingleNestedEntity(
                        nestedEntity, targetEntityType, targetEntitySet,
                        clrPropertyName, parentItem, isCreation, currentDepth);
                }
                else if (value is IEnumerable collection && value is not string)
                {
                    foreach (var item in collection)
                    {
                        if (item is EdmEntityObject collectionEntity)
                        {
                            ProcessSingleNestedEntity(
                                collectionEntity, targetEntityType, targetEntitySet,
                                clrPropertyName, parentItem, isCreation, currentDepth);
                        }
                    }
                }
            }
        }

        private void ProcessSingleNestedEntity(
            EdmEntityObject nestedEntity,
            IEdmEntityType targetEntityType,
            string targetEntitySetName,
            string clrNavPropertyName,
            DataModificationItem parentItem,
            bool isCreation,
            int currentDepth)
        {
            if (IsEntityReference(nestedEntity))
            {
                var bindRef = CreateBindReference(nestedEntity, targetEntityType, targetEntitySetName);
                if (!parentItem.NavigationBindings.TryGetValue(clrNavPropertyName, out var bindList))
                {
                    bindList = new List<BindReference>();
                    parentItem.NavigationBindings[clrNavPropertyName] = bindList;
                }

                bindList.Add(bindRef);
                return;
            }

            var actualEdmType = nestedEntity.ActualEdmType as IEdmStructuredType ?? targetEntityType;
            var clrType = actualEdmType.GetClrType(model);

            var childItem = new DataModificationItem(
                targetEntitySetName,
                targetEntityType.GetClrType(model),
                clrType,
                isCreation ? RestierEntitySetOperation.Insert : RestierEntitySetOperation.Update,
                isCreation ? null : ExtractKeyValues(nestedEntity, targetEntityType),
                null,
                nestedEntity.CreatePropertyDictionary(actualEdmType, api, isCreation))
            {
                ParentItem = parentItem,
                ParentNavigationPropertyName = clrNavPropertyName,
            };

            parentItem.NestedItems.Add(childItem);
            ExtractNestedItems(nestedEntity, actualEdmType, childItem, isCreation, currentDepth + 1);
        }

        private static bool IsEntityReference(EdmEntityObject entity)
        {
            // Check for OData ID annotation — entity references from @odata.bind
            if (entity.TryGetPropertyValue("@odata.id", out _))
            {
                return true;
            }

            return false;
        }

        private BindReference CreateBindReference(
            EdmEntityObject entity,
            IEdmEntityType entityType,
            string entitySetName)
        {
            return new BindReference
            {
                ResourceSetName = entitySetName,
                ResourceKey = ExtractKeyValues(entity, entityType),
            };
        }

        private IReadOnlyDictionary<string, object> ExtractKeyValues(
            EdmEntityObject entity,
            IEdmEntityType entityType)
        {
            var keys = new Dictionary<string, object>();
            foreach (var keyProperty in entityType.Key())
            {
                if (entity.TryGetPropertyValue(keyProperty.Name, out var value))
                {
                    var clrName = EdmClrPropertyMapper.GetClrPropertyName(keyProperty, model);
                    keys[clrName] = value;
                }
            }

            return keys;
        }

        private string FindTargetEntitySet(IEdmNavigationProperty navProperty)
        {
            var container = model.EntityContainer;
            if (container is not null)
            {
                foreach (var entitySet in container.EntitySets())
                {
                    var navigationTarget = entitySet.FindNavigationTarget(navProperty);
                    if (navigationTarget is not null)
                    {
                        return navigationTarget.Name;
                    }
                }
            }

            return navProperty.ToEntityType().Name;
        }
    }
}
