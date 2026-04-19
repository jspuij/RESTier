// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using System.Collections.Generic;


#if EF6
using System.Data.Entity;

namespace Microsoft.Restier.EntityFramework
#endif

#if EFCore
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Restier.EntityFrameworkCore
#endif

{
    /// <summary>
    /// Represents a model producer that uses the metadata workspace accessible from a <see cref="DbContext" />.
    /// </summary>
    public partial class EFModelBuilder<TDbContext> : IModelBuilder
        where TDbContext : DbContext
    {
        private readonly TDbContext _dbContext;
        private readonly ModelMerger _modelMerger;
        private readonly RestierNamingConvention _namingConvention;

        /// <summary>
        /// Initializes a new instance of the <see cref="EFModelBuilder{TDbContext}"/> class.
        /// </summary>
        /// <param name="dbContext">The DbContext to use for model building.</param>
        /// <param name="modelMerger">The model merger to use.</param>
        /// <param name="namingConvention">The naming convention to use for the EDM model.</param>
        public EFModelBuilder(TDbContext dbContext, ModelMerger modelMerger, RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase)
        {
            Ensure.NotNull(dbContext, nameof(dbContext));
            Ensure.NotNull(modelMerger, nameof(modelMerger));
            this._dbContext = dbContext;
            this._modelMerger = modelMerger;
            this._namingConvention = namingConvention;
        }

        /// <summary>
        /// A way to chain ModelBuilders together.
        /// </summary>
        public IModelBuilder Inner { get; set; }

        /// <inheritdoc />
        public IEdmModel GetEdmModel()
        {
            // Get the Entity set maps from the respective EF versions.
#if EFCore

            EntityFrameworkCoreGetEntities(out var entitySetMap, out var entitySetKeyMap);
#endif
#if EF6
            EntityFramework6GetEntitySets(out var entitySetMap, out var entitySetKeyMap);
#endif
            // Get the inner model if it exists.
            var innerModel = Inner?.GetEdmModel();

            // Build the model from the Entity Framework Entity Sets.
            var result = BuildEdmModelFromEntitySetMaps(entitySetMap, entitySetKeyMap, _namingConvention);

            // merge the inner model into the result.
            if (innerModel is not null)
            {
                _modelMerger.Merge(innerModel, result);
            }

            return result;
        }

        private static EdmModel BuildEdmModelFromEntitySetMaps(Dictionary<string, Type> entitySetMap, Dictionary<Type, ICollection<PropertyInfo>> entitySetKeyMap, RestierNamingConvention namingConvention)
        {
            if (!entitySetMap.Any())
            {
                return new EdmModel();
            }

            // Collection of entity type and set name is set by EF now,
            // and EF model producer will not build model any more
            // Web Api OData conversion model built is been used here,
            // refer to Web Api OData document for the detail conversions been used for model built.
            var builder = new ODataConventionModelBuilder
            {
                // This namespace is used by container
                Namespace = entitySetMap.First().Value.Namespace
            };

            var method = typeof(ODataConventionModelBuilder).GetMethod("EntitySet", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            foreach (var pair in entitySetMap)
            {
                // Build a method with the specific type argument
                var specifiedMethod = method.MakeGenericMethod(pair.Value);
                var parameters = new object[]
                {
                    pair.Key,
                };

                specifiedMethod.Invoke(builder, parameters);
            }

            foreach (var pair in entitySetKeyMap)
            {
                if (builder.GetTypeConfigurationOrNull(pair.Key) is not EntityTypeConfiguration edmTypeConfiguration)
                {
                    continue;
                }

                if (pair.Value is null)
                {
                    throw new InvalidOperationException($"The entity '{pair.Key}' does not have a key specified. Entities tagged with the [Keyless] attribute " +
                                                        $"(or otherwise do not have a key specified) are not supported in either OData or Restier. Please map the object as a ComplexType and " +
                                                        $"implement as an [UnboundOperation] on your API instead.");
                }

                foreach (var property in pair.Value)
                {
                    edmTypeConfiguration.HasKey(property);
                }
            }
            switch (namingConvention)
            {
                case RestierNamingConvention.LowerCamelCase:
                    builder.EnableLowerCamelCase();
                    break;
                case RestierNamingConvention.LowerCamelCaseWithEnumMembers:
                    builder.EnableLowerCamelCaseForPropertiesAndEnums();
                    break;
            }

            return (EdmModel)builder.GetEdmModel();
        }
    }
}
