// <copyright file="ConventionBasedMethodNameFactoryTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using FluentAssertions;
    using Microsoft.OData.Edm;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Operation;
    using Microsoft.Restier.Core.Submit;
    using Microsoft.Restier.Tests.Shared;
    using Moq;
    using Xunit;

    /// <summary>
    /// Unit tests for the <see cref="ConventionBasedMethodNameFactory"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ConventionBasedMethodNameFactoryTests : IClassFixture<ServiceProviderFixture>
    {
        private readonly IServiceProvider serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConventionBasedMethodNameFactoryTests"/> class.
        /// </summary>
        /// <param name="serviceProviderFixture">A fixture for <see cref="IServiceProvider"/>.</param>
        public ConventionBasedMethodNameFactoryTests(ServiceProviderFixture serviceProviderFixture)
        {
            this.serviceProvider = serviceProviderFixture.ServiceProvider;
        }

        /// <summary>
        /// Tests all posibilities for GetEntitySetMethodName.
        /// </summary>
        /// <param name="pipelineState">The pipeline state.</param>
        /// <param name="entitySetOperation">The entity set operation.</param>
        /// <param name="expected">The expected result.</param>
        [Theory]
        [ClassData(typeof(MethodNameData))]
        public static void CanCallGetEntitySetMethodNameWithEntitySetAndRestierPipelineStateAndOperation(
            RestierPipelineState pipelineState,
            RestierEntitySetOperation entitySetOperation,
            string expected)
        {
            var entitySetMock = new Mock<IEdmEntitySet>();
            var entityCollectionTypeMock = new Mock<IEdmCollectionType>();
            var entityTypeReferenceMock = new Mock<IEdmEntityTypeReference>();
            var entityTypeMock = new Mock<IEdmEntityType>();

            entityTypeMock.Setup(x => x.Name).Returns("Test");
            entityTypeReferenceMock.Setup(x => x.Definition).Returns(entityTypeMock.Object);
            entityCollectionTypeMock.Setup(x => x.ElementType).Returns(entityTypeReferenceMock.Object);
            entitySetMock.Setup(x => x.Name).Returns("Tests");
            entitySetMock.Setup(x => x.Type).Returns(entityCollectionTypeMock.Object);

            var result = ConventionBasedMethodNameFactory.GetEntitySetMethodName(entitySetMock.Object, pipelineState, entitySetOperation);
            result.Should().Be(expected);
        }

        /// <summary>
        /// Checks that calling GetEntitySetMethodName with a null IEdmEntitySet returns an empty string.
        /// </summary>
        [Fact]
        public static void CanCallGetEntitySetMethodNameWithEntitySetAndRestierPipelineStateAndOperationWithNullEntitySet()
        {
            var result = ConventionBasedMethodNameFactory.GetEntitySetMethodName(
                default(IEdmEntitySet),
                RestierPipelineState.PostSubmit,
                RestierEntitySetOperation.Insert);
            result.Should().Be(string.Empty);
        }

        /// <summary>
        /// Tests all possibilities for GetEntitySetMethodName.
        /// </summary>
        /// <param name="pipelineState">The pipeline state.</param>
        /// <param name="entitySetOperation">The entity set operation.</param>
        /// <param name="expected">The expected result.</param>
        [Theory]
        [ClassData(typeof(MethodNameData))]
        public static void CanCallGetEntitySetMethodNameWithItemAndRestierPipelineState(
            RestierPipelineState pipelineState,
            RestierEntitySetOperation entitySetOperation,
            string expected)
        {
            var item = new DataModificationItem(
                "Tests",
                typeof(Test),
                typeof(Test),
                entitySetOperation,
                new Mock<IReadOnlyDictionary<string, object>>().Object,
                new Mock<IReadOnlyDictionary<string, object>>().Object,
                new Mock<IReadOnlyDictionary<string, object>>().Object);
            var result = ConventionBasedMethodNameFactory.GetEntitySetMethodName(item, pipelineState);
            result.Should().Be(expected);
        }

        /// <summary>
        /// Checks that calling GetEntitySetMethodName with a null DataModificationItem returns an empty string.
        /// </summary>
        [Fact]
        public static void CanCallGetEntitySetMethodNameWithItemAndRestierPipelineStateWithNullItem()
        {
            var result = ConventionBasedMethodNameFactory.GetEntitySetMethodName(
                default(DataModificationItem),
                RestierPipelineState.Authorization);
            result.Should().Be(string.Empty);
        }

        /// <summary>
        /// Tests all posibilities for GetFunctionMethodName.
        /// </summary>
        /// <param name="pipelineState">The pipeline state.</param>
        /// <param name="expected">The expected result.</param>
        [Theory]
        [InlineData(RestierPipelineState.Authorization, "CanExecuteCalculate")]
        [InlineData(RestierPipelineState.PostSubmit, "OnExecutedCalculate")]
        [InlineData(RestierPipelineState.PreSubmit, "OnExecutingCalculate")]
        [InlineData(RestierPipelineState.Submit, "")]
        [InlineData(RestierPipelineState.Validation, "")]
        public static void CanCallGetFunctionMethodNameWithIEdmOperationImportAndRestierPipelineStateAndRestierOperationMethod(
            RestierPipelineState pipelineState,
            string expected)
        {
            var operationImportMock = new Mock<IEdmOperationImport>();
            var operationMock = new Mock<IEdmOperation>();
            operationMock.Setup(x => x.Name).Returns("Calculate");
            operationImportMock.Setup(x => x.Operation).Returns(operationMock.Object);
            var restierOperation = RestierOperationMethod.Execute;
            var result = ConventionBasedMethodNameFactory.GetFunctionMethodName(operationImportMock.Object, pipelineState, restierOperation);
            result.Should().Be(expected);
        }

        /// <summary>
        /// Checks that calling GetFunctionMethodName with a null IEdmOperationImport returns an empty string.
        /// </summary>
        [Fact]
        public static void CanCallGetFunctionMethodNameWithIEdmOperationImportAndRestierPipelineStateAndRestierOperationMethodWithNullOperationImport()
        {
            var result = ConventionBasedMethodNameFactory.GetFunctionMethodName(
                default(IEdmOperationImport),
                RestierPipelineState.PostSubmit,
                RestierOperationMethod.Execute);
            result.Should().Be(string.Empty);
        }

        /// <summary>
        /// Checks that calling GetFunctionMethodName with a null OperationContext returns an empty string.
        /// </summary>
        [Fact]
        public static void CannotCallGetFunctionMethodNameWithOperationContextAndRestierPipelineStateAndRestierOperationMethodWithNullOperationImport()
        {
            var result = ConventionBasedMethodNameFactory.GetFunctionMethodName(
                default(OperationContext),
                RestierPipelineState.Authorization,
                RestierOperationMethod.Execute);
            result.Should().Be(string.Empty);
        }

        /// <summary>
        /// Tests all posibilities for GetFunctionMethodName.
        /// </summary>
        /// <param name="pipelineState">The pipeline state.</param>
        /// <param name="expected">The expected result.</param>
        [Theory]
        [InlineData(RestierPipelineState.Authorization, "CanExecuteCalculate")]
        [InlineData(RestierPipelineState.PostSubmit, "OnExecutedCalculate")]
        [InlineData(RestierPipelineState.PreSubmit, "OnExecutingCalculate")]
        [InlineData(RestierPipelineState.Submit, "")]
        [InlineData(RestierPipelineState.Validation, "")]
        public void CanCallGetFunctionMethodNameWithOperationContextAndRestierPipelineStateAndRestierOperationMethod(
            RestierPipelineState pipelineState,
            string expected)
        {
            var operationImport = new OperationContext(
                new EmptyApi(this.serviceProvider),
                name => this,
                "Calculate",
                false,
                new Mock<IEnumerable>().Object);
            var restierOperation = RestierOperationMethod.Execute;
            var result = ConventionBasedMethodNameFactory.GetFunctionMethodName(operationImport, pipelineState, restierOperation);
            result.Should().Be(expected);
        }

        private class MethodNameData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { RestierPipelineState.Authorization, RestierEntitySetOperation.Delete, "CanDeleteTest" };
                yield return new object[] { RestierPipelineState.PostSubmit, RestierEntitySetOperation.Delete, "OnDeletedTest" };
                yield return new object[] { RestierPipelineState.PreSubmit, RestierEntitySetOperation.Delete, "OnDeletingTest" };
                yield return new object[] { RestierPipelineState.Submit, RestierEntitySetOperation.Delete, string.Empty };
                yield return new object[] { RestierPipelineState.Validation, RestierEntitySetOperation.Delete, string.Empty };
                yield return new object[] { RestierPipelineState.Authorization, RestierEntitySetOperation.Filter, string.Empty };
                yield return new object[] { RestierPipelineState.PostSubmit, RestierEntitySetOperation.Filter, string.Empty };
                yield return new object[] { RestierPipelineState.PreSubmit, RestierEntitySetOperation.Filter, string.Empty };
                yield return new object[] { RestierPipelineState.Submit, RestierEntitySetOperation.Filter, "OnFilterTests" };
                yield return new object[] { RestierPipelineState.Validation, RestierEntitySetOperation.Filter, string.Empty };
                yield return new object[] { RestierPipelineState.Authorization, RestierEntitySetOperation.Insert, "CanInsertTest" };
                yield return new object[] { RestierPipelineState.PostSubmit, RestierEntitySetOperation.Insert, "OnInsertedTest" };
                yield return new object[] { RestierPipelineState.PreSubmit, RestierEntitySetOperation.Insert, "OnInsertingTest" };
                yield return new object[] { RestierPipelineState.Submit, RestierEntitySetOperation.Insert, string.Empty };
                yield return new object[] { RestierPipelineState.Validation, RestierEntitySetOperation.Insert, string.Empty };
                yield return new object[] { RestierPipelineState.Authorization, RestierEntitySetOperation.Update, "CanUpdateTest" };
                yield return new object[] { RestierPipelineState.PostSubmit, RestierEntitySetOperation.Update, "OnUpdatedTest" };
                yield return new object[] { RestierPipelineState.PreSubmit, RestierEntitySetOperation.Update, "OnUpdatingTest" };
                yield return new object[] { RestierPipelineState.Submit, RestierEntitySetOperation.Update, string.Empty };
                yield return new object[] { RestierPipelineState.Validation, RestierEntitySetOperation.Update, string.Empty };
            }

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        }

        private class Test
        {
        }

        private class EmptyApi : ApiBase
        {
            public EmptyApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }
        }
    }
}