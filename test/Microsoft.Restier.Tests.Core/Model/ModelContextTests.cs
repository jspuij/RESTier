// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Xunit;

namespace Microsoft.Restier.Tests.Core.Model
{
    /// <summary>
    /// Unit tests for the <see cref="ModelContext"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ModelContextTests
    {
        private ModelContext testClass;
        private ApiBase api;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelContextTests"/> class.
        /// </summary>
         public ModelContextTests()
        {
            api = new TestApi(
                Substitute.For<IEdmModel>(),
                Substitute.For<IQueryHandler>(),
                Substitute.For<ISubmitHandler>());
            testClass = new ModelContext(api);
        }

        /// <summary>
        /// Tests that a model context can be constructed.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var instance = new ModelContext(api);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that a model context cannot be constructed without an ApiBase.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullApi()
        {
            Action act = () => new ModelContext(default(ApiBase));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Tests that the ResourceMap can be retrieved.
        /// </summary>
        [Fact]
        public void CanGetResourceSetTypeMap()
        {
            testClass.ResourceSetTypeMap.Should().BeAssignableTo<IDictionary<string, Type>>();
        }

        /// <summary>
        /// Tests that the ResourceTypeKeyPropertiesMap can be retreived.
        /// </summary>
        [Fact]
        public void CanGetResourceTypeKeyPropertiesMap()
        {
            testClass.ResourceTypeKeyPropertiesMap.Should().BeAssignableTo<IDictionary<Type, ICollection<PropertyInfo>>>();
        }

        private class TestApi : ApiBase
        {
            public TestApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }
        }
    }
}