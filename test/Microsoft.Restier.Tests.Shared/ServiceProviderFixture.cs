// <copyright file="ServiceProviderFixture.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.OData.Edm;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Model;
    using Microsoft.Restier.Core.Query;
    using Microsoft.Restier.Core.Submit;
    using Moq;

    /// <summary>
    /// A fixture to setup an IServiceProvider instance that contains all the neccessary this.ServiceProviderMocks.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ServiceProviderFixture
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceProviderFixture"/> class.
        /// </summary>
        public ServiceProviderFixture()
        {
            this.ServiceProviderMock = new Mock<IServiceProvider>();

            this.ServiceProviderMock.Setup(x => x.GetService(typeof(IQueryExpressionSourcer))).Returns(new Mock<IQueryExpressionSourcer>().Object);

            this.QueryExpressionAuthorizer = new Mock<IQueryExpressionAuthorizer>();

            // authorize any query as default.
            this.QueryExpressionAuthorizer.Setup(x => x.Authorize(It.IsAny<QueryExpressionContext>())).Returns(true);

            this.ServiceProviderMock.Setup(x => x.GetService(typeof(IQueryExpressionAuthorizer))).Returns(this.QueryExpressionAuthorizer.Object);
            this.ServiceProviderMock.Setup(x => x.GetService(typeof(IQueryExpressionExpander))).Returns(new Mock<IQueryExpressionExpander>().Object);

            this.QueryExpressionProcessor = new Mock<IQueryExpressionProcessor>();

            // just pass on the visited node without filter as default.
            this.QueryExpressionProcessor.Setup(x => x.Process(It.IsAny<QueryExpressionContext>())).Returns<QueryExpressionContext>(q => q.VisitedNode);

            this.ServiceProviderMock.Setup(x => x.GetService(typeof(IQueryExpressionProcessor))).Returns(this.QueryExpressionProcessor.Object);

            this.QueryExecutor = new Mock<IQueryExecutor>();

            this.ServiceProviderMock.Setup(x => x.GetService(typeof(IQueryExecutor))).Returns(this.QueryExecutor.Object);

            this.ChangeSetInitializer = new Mock<IChangeSetInitializer>();

            this.ServiceProviderMock.Setup(x => x.GetService(typeof(IChangeSetInitializer))).Returns(this.ChangeSetInitializer.Object);

            this.ChangeSetItemAuthorizer = new Mock<IChangeSetItemAuthorizer>();

            this.ServiceProviderMock.Setup(x => x.GetService(typeof(IChangeSetItemAuthorizer))).Returns(this.ChangeSetItemAuthorizer.Object);

            this.ChangeSetItemValidator = new Mock<IChangeSetItemValidator>();

            this.ServiceProviderMock.Setup(x => x.GetService(typeof(IChangeSetItemValidator))).Returns(this.ChangeSetItemValidator.Object);

            this.ChangeSetItemFilter = new Mock<IChangeSetItemFilter>();

            this.ServiceProviderMock.Setup(x => x.GetService(typeof(IChangeSetItemFilter))).Returns(this.ChangeSetItemFilter.Object);

            this.SubmitExecutor = new Mock<ISubmitExecutor>();

            var submitResult = new SubmitResult(new ChangeSet());

            // return the result from the context as default operation.
            this.SubmitExecutor.Setup(x => x.ExecuteSubmitAsync(It.IsAny<SubmitContext>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(submitResult));

            this.ServiceProviderMock.Setup(x => x.GetService(typeof(ISubmitExecutor))).Returns(this.SubmitExecutor.Object);

            this.ModelBuilder = new Mock<IModelBuilder>();

            var edmModel = new Mock<IEdmModel>().Object;

            // return the edm model as default.
            this.ModelBuilder.Setup(x => x.GetModelAsync(It.IsAny<ModelContext>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(edmModel));

            this.ServiceProviderMock.Setup(x => x.GetService(typeof(IModelBuilder))).Returns(this.ModelBuilder.Object);
            this.ModelMapper = new Mock<IModelMapper>();
            this.ServiceProviderMock.Setup(x => x.GetService(typeof(IModelMapper))).Returns(this.ModelMapper.Object);

            var propertyBag = new PropertyBag();
            this.ServiceProviderMock.Setup(x => x.GetService(typeof(PropertyBag))).Returns(propertyBag);

            var apiConfiguration = new ApiConfiguration();

            this.ServiceProviderMock.Setup(x => x.GetService(typeof(ApiConfiguration))).Returns(apiConfiguration);

            this.ServiceProvider = this.ServiceProviderMock.Object;
        }

        /// <summary>
        /// Gets the this.ServiceProviderMock for IModelMapper.
        /// </summary>
        public Mock<IServiceProvider> ServiceProviderMock { get; private set; }

        /// <summary>
        /// Gets the this.ServiceProviderMocked <see cref="IServiceProvider"/> instance.
        /// </summary>
        public IServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        /// Gets the this.ServiceProviderMock for IModelMapper.
        /// </summary>
        public Mock<IModelMapper> ModelMapper { get; private set; }

        /// <summary>
        /// Gets the this.ServiceProviderMock for the ModelBuilder.
        /// </summary>
        public Mock<IModelBuilder> ModelBuilder { get; private set; }

        /// <summary>
        /// Gets the this.ServiceProviderMock for the QueryExpressionAuthorizer.
        /// </summary>
        public Mock<IQueryExpressionAuthorizer> QueryExpressionAuthorizer { get; private set; }

        /// <summary>
        /// Gets the this.ServiceProviderMock for the QueryExpressionProcessor.
        /// </summary>
        public Mock<IQueryExpressionProcessor> QueryExpressionProcessor { get; }

        /// <summary>
        /// Gets the this.ServiceProviderMock for the QueryExecutor.
        /// </summary>
        public Mock<IQueryExecutor> QueryExecutor { get; }

        /// <summary>
        /// Gets the this.ServiceProviderMock for the ChangeSetInitializer.
        /// </summary>
        public Mock<IChangeSetInitializer> ChangeSetInitializer { get; private set; }

        /// <summary>
        /// Gets the this.ServiceProviderMock for the ChangeSetItemValidator.
        /// </summary>
        public Mock<IChangeSetItemValidator> ChangeSetItemValidator { get; private set; }

        /// <summary>
        /// Gets the this.ServiceProviderMock for the ChangeSetItemAuthorizer.
        /// </summary>
        public Mock<IChangeSetItemAuthorizer> ChangeSetItemAuthorizer { get; private set; }

        /// <summary>
        /// Gets the this.ServiceProviderMock for the ChangeSetItemFilter.
        /// </summary>
        public Mock<IChangeSetItemFilter> ChangeSetItemFilter { get; private set; }

        /// <summary>
        /// Gets the this.ServiceProviderMock for the Submit executor.
        /// </summary>
        public Mock<ISubmitExecutor> SubmitExecutor { get; private set; }
    }
}
