// <copyright file="DataSourceStubModelReferenceTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Tests.Query
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Expressions;
    using FluentAssertions;
    using Microsoft.OData.Edm;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Query;
    using Microsoft.Restier.Tests.Shared;
    using Moq;
    using Xunit;

    /// <summary>
    /// Unit tests for the <see cref="DataSourceStubModelReference"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DataSourceStubModelReferenceTests : IClassFixture<ServiceProviderFixture>
    {
        private readonly ServiceProviderFixture serviceProviderFixture;
        private readonly IQueryable<Test> queryable = new List<Test>()
        {
            new Test() { Name = "The" },
            new Test() { Name = "Quick" },
            new Test() { Name = "Brown" },
            new Test() { Name = "Fox" },
        }.AsQueryable();

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSourceStubModelReferenceTests"/> class.
        /// </summary>
        /// <param name="serviceProviderFixture">The <see cref="IServiceProvider"/> fixture.</param>
        public DataSourceStubModelReferenceTests(ServiceProviderFixture serviceProviderFixture)
        {
            this.serviceProviderFixture = serviceProviderFixture;
        }

        /// <summary>
        /// Tests whether the DataSourceStubModelReference can be constructed.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var queryContext = new QueryContext(
                new TestApi(this.serviceProviderFixture.ServiceProvider),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(this.queryable))));
            var testClass = new DataSourceStubModelReference(
                queryContext, "Tests");
            testClass.Should().NotBeNull();
        }

        /// <summary>
        /// Tests whether the DataSourceStubModelReference can be constructed.
        /// </summary>
        [Fact]
        public void CanConstructWithNamespace()
        {
            var queryContext = new QueryContext(
                new TestApi(this.serviceProviderFixture.ServiceProvider),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(this.queryable))));
            var testClass = new DataSourceStubModelReference(
                queryContext, "Microsoft.Restier.Core.Tests.Query", "Tests");
            testClass.Should().NotBeNull();
        }

        /// <summary>
        /// Can Get an EntitySet.
        /// </summary>
        [Fact]
        public void CanGetEntitySet()
        {
            var modelMock = new Mock<IEdmModel>();
            var entityContainerMock = new Mock<IEdmEntityContainer>();
            var list = new List<IEdmEntityContainerElement>();
            var entityContainerElementItemMock = new Mock<IEdmEntityContainerElement>();
            entityContainerElementItemMock.Setup(x => x.Name).Returns("Tests");
            var edmEntitySetMock = entityContainerElementItemMock.As<IEdmEntitySet>();
            list.Add(entityContainerElementItemMock.Object);

            modelMock.Setup(x => x.EntityContainer).Returns(entityContainerMock.Object);
            entityContainerMock.Setup(x => x.Elements).Returns(list);

            var queryContext = new QueryContext(
                new TestApi(this.serviceProviderFixture.ServiceProvider),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(this.queryable))))
            {
                Model = modelMock.Object,
            };
            var testClass = new DataSourceStubModelReference(
                queryContext, "Tests");

            testClass.EntitySet.Should().BeAssignableTo<IEdmEntitySet>();
        }

        /// <summary>
        /// Cannot get an EntitySet.
        /// </summary>
        [Fact]
        public void CannotGetEntitySet()
        {
            var modelMock = new Mock<IEdmModel>();
            var entityContainerMock = new Mock<IEdmEntityContainer>();
            var list = new List<IEdmEntityContainerElement>();
            var entityContainerElementItemMock = new Mock<IEdmEntityContainerElement>();
            entityContainerElementItemMock.Setup(x => x.Name).Returns("Tests");
            list.Add(entityContainerElementItemMock.Object);

            modelMock.Setup(x => x.EntityContainer).Returns(entityContainerMock.Object);
            entityContainerMock.Setup(x => x.Elements).Returns(list);

            var queryContext = new QueryContext(
                new TestApi(this.serviceProviderFixture.ServiceProvider),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(this.queryable))))
            {
                Model = modelMock.Object,
            };
            var testClass = new DataSourceStubModelReference(
                queryContext, "Tests");

            testClass.EntitySet.Should().BeNull();
        }

        /// <summary>
        /// Can get the Edm Type from an IEdmNavigationSource.
        /// </summary>
        [Fact]
        public void CanGetTypeIEdmNavigationSource()
        {
            var modelMock = new Mock<IEdmModel>();
            var entityContainerMock = new Mock<IEdmEntityContainer>();
            var list = new List<IEdmEntityContainerElement>();
            var entityContainerElementItemMock = new Mock<IEdmEntityContainerElement>();
            entityContainerElementItemMock.Setup(x => x.Name).Returns("Tests");
            var source = entityContainerElementItemMock.As<IEdmNavigationSource>();
            var edmType = new Mock<IEdmType>().Object;

            source.Setup(x => x.Type).Returns(edmType);
            list.Add(entityContainerElementItemMock.Object);

            modelMock.Setup(x => x.EntityContainer).Returns(entityContainerMock.Object);
            entityContainerMock.Setup(x => x.Elements).Returns(list);

            var queryContext = new QueryContext(
                new TestApi(this.serviceProviderFixture.ServiceProvider),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(this.queryable))))
            {
                Model = modelMock.Object,
            };
            var testClass = new DataSourceStubModelReference(
                queryContext, "Tests");

            testClass.Type.Should().BeAssignableTo<IEdmType>();
            testClass.Type.Should().Be(edmType);
        }

        /// <summary>
        /// Can get the Edm Type from an IEdmFunctionImport.
        /// </summary>
        [Fact]
        public void CanGetTypeIEdmFunctionImport()
        {
            var modelMock = new Mock<IEdmModel>();
            var entityContainerMock = new Mock<IEdmEntityContainer>();
            var list = new List<IEdmEntityContainerElement>();
            var entityContainerElementItemMock = new Mock<IEdmEntityContainerElement>();
            entityContainerElementItemMock.Setup(x => x.Name).Returns("Tests");
            var source = entityContainerElementItemMock.As<IEdmFunctionImport>();
            var edmType = new Mock<IEdmType>().Object;

            source.Setup(x => x.Function.ReturnType.Definition).Returns(edmType);
            list.Add(entityContainerElementItemMock.Object);

            modelMock.Setup(x => x.EntityContainer).Returns(entityContainerMock.Object);
            entityContainerMock.Setup(x => x.Elements).Returns(list);

            var queryContext = new QueryContext(
                new TestApi(this.serviceProviderFixture.ServiceProvider),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(this.queryable))))
            {
                Model = modelMock.Object,
            };
            var testClass = new DataSourceStubModelReference(
                queryContext, "Tests");

            testClass.Type.Should().BeAssignableTo<IEdmType>();
            testClass.Type.Should().Be(edmType);
        }

        /// <summary>
        /// Can get the Edm Type from an IEdmFunction.
        /// </summary>
        [Fact]
        public void CanGetTypeIEdmFunction()
        {
            var modelMock = new Mock<IEdmModel>();
            var entityContainerMock = new Mock<IEdmEntityContainer>();
            var list = new List<IEdmSchemaElement>();
            var schemaElementMock = new Mock<IEdmSchemaElement>();
            schemaElementMock.Setup(x => x.Name).Returns("Tests");
            schemaElementMock.Setup(x => x.Namespace).Returns("Microsoft.Restier.Core.Tests.Query");
            var source = schemaElementMock.As<IEdmFunction>();
            var edmType = new Mock<IEdmType>().Object;

            source.Setup(x => x.ReturnType.Definition).Returns(edmType);
            list.Add(schemaElementMock.Object);

            modelMock.Setup(x => x.SchemaElements).Returns(list);

            var queryContext = new QueryContext(
                new TestApi(this.serviceProviderFixture.ServiceProvider),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(this.queryable))))
            {
                Model = modelMock.Object,
            };
            var testClass = new DataSourceStubModelReference(
                queryContext, "Microsoft.Restier.Core.Tests.Query", "Tests");

            testClass.Type.Should().BeAssignableTo<IEdmType>();
            testClass.Type.Should().Be(edmType);
        }

        /// <summary>
        /// Cannot get the Edm Type.
        /// </summary>
        [Fact]
        public void CannotGetType()
        {
            var modelMock = new Mock<IEdmModel>();
            var entityContainerMock = new Mock<IEdmEntityContainer>();
            var list = new List<IEdmEntityContainerElement>();
            var entityContainerElementItemMock = new Mock<IEdmEntityContainerElement>();
            entityContainerElementItemMock.Setup(x => x.Name).Returns("Tests");
            list.Add(entityContainerElementItemMock.Object);

            modelMock.Setup(x => x.EntityContainer).Returns(entityContainerMock.Object);
            entityContainerMock.Setup(x => x.Elements).Returns(list);

            var queryContext = new QueryContext(
                new TestApi(this.serviceProviderFixture.ServiceProvider),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(this.queryable))))
            {
                Model = modelMock.Object,
            };
            var testClass = new DataSourceStubModelReference(
                queryContext, "Tests");

            testClass.Type.Should().BeNull();
        }

        /// <summary>
        /// Can get an element.
        /// </summary>
        [Fact]
        public void CanGetElement()
        {
            var modelMock = new Mock<IEdmModel>();
            var entityContainerMock = new Mock<IEdmEntityContainer>();
            var list = new List<IEdmEntityContainerElement>();
            var entityContainerElementItemMock = new Mock<IEdmEntityContainerElement>();
            entityContainerElementItemMock.Setup(x => x.Name).Returns("Tests");
            list.Add(entityContainerElementItemMock.Object);

            modelMock.Setup(x => x.EntityContainer).Returns(entityContainerMock.Object);
            entityContainerMock.Setup(x => x.Elements).Returns(list);

            var queryContext = new QueryContext(
                new TestApi(this.serviceProviderFixture.ServiceProvider),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(this.queryable))))
            {
                Model = modelMock.Object,
            };
            var testClass = new DataSourceStubModelReference(
                queryContext, "Tests");

            testClass.Element.Should().BeAssignableTo<IEdmElement>();
        }

        /// <summary>
        /// Can get an element.
        /// </summary>
        [Fact]
        public void CannotGetElement()
        {
            var modelMock = new Mock<IEdmModel>();
            var entityContainerMock = new Mock<IEdmEntityContainer>();
            var list = new List<IEdmEntityContainerElement>();
            var entityContainerElementItemMock = new Mock<IEdmEntityContainerElement>();
            entityContainerElementItemMock.Setup(x => x.Name).Returns("Testing");
            list.Add(entityContainerElementItemMock.Object);

            modelMock.Setup(x => x.EntityContainer).Returns(entityContainerMock.Object);
            entityContainerMock.Setup(x => x.Elements).Returns(list);

            var queryContext = new QueryContext(
                new TestApi(this.serviceProviderFixture.ServiceProvider),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(this.queryable))))
            {
                Model = modelMock.Object,
            };
            var testClass = new DataSourceStubModelReference(
                queryContext, "Tests");

            testClass.Element.Should().BeNull();
        }

        private class TestApi : ApiBase
        {
            public TestApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }
        }

        private class Test
        {
            public string Name { get; set; }
        }
    }
}