// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.EntityFrameworkCore;

/// <summary>
/// Represents a model producer that uses the metadata workspace accessible from a <see cref="DbContext" />.
/// </summary>
public partial class EFModelBuilder<TDbContext> : IModelBuilder
    where TDbContext : DbContext
{
    private void EntityFrameworkCoreGetEntities(out Dictionary<string, Type> entitySetMap, out Dictionary<Type, ICollection<PropertyInfo>> entitySetKeyMap)
    {
        // @robertmclaws: Validate that no Owned Types are mapped to DbSet<>. If there are, EFCore calls to GetModel will fail.
        var ownedTypes = _dbContext.Model.GetEntityTypes().Where(c => c.IsOwned()).ToList();
        var dbSetMappedTypes = ownedTypes.Where(c => _dbContext.IsDbSetMapped(c.ClrType)).ToList();

        if (dbSetMappedTypes.Count > 0)
        {
            throw new EdmModelValidationException($"The '{_dbContext.GetType().Name}' DbContext has 'Owned Types' (the EFCore equivalent of EF6's 'Complex Types') mapped to DbSets. " +
                                                  $"You must remove the following DbSet mappings for EFCore to function properly with Restier: {string.Join(",", dbSetMappedTypes.Select(c => c.ShortName()))}");
        }

        // @caldwell0414: This code is looking for all the DBSets on the context and generating a dictionary of DbSet Name and the Entity type.
        entitySetMap = _dbContext.GetType().GetProperties()
            .Where(e => e.PropertyType.FindGenericType(typeof(DbSet<>)) is not null)
            .ToDictionary(e => e.Name, e => e.PropertyType.GetGenericArguments()[0]);

        // @caldwell0414: This code goes through all the Entity types in the model, and where not marked as "owned" builds a dictionary of name and primary-key type.

            entitySetKeyMap = _dbContext.Model.GetEntityTypes().Where(c => !c.IsOwned() && !IsImplicitManyToManyJoinEntity(c)).ToDictionary(
                            e => e.ClrType,
                            e => ((ICollection<PropertyInfo>)e.FindPrimaryKey()?.Properties.Select(p => e.ClrType?.GetProperty(p.Name)).ToList()));
    }

    /// <summary>
    /// A replacement for IsImplicitlyCreatedJoinEntityType, since on EF Core 6.0 Model.GetEntityTypes() returns RuntimeEntityTypes instead of EntityTypes.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    private static bool IsImplicitManyToManyJoinEntity(IEntityType entity) =>
        entity.ClrType == typeof(Dictionary<string, object>) && entity.GetForeignKeys().Count() == 2 && entity.GetProperties().Count() == 2;
}