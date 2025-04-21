// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Batch;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using NSubstitute;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Extensions
{
    /// <summary>
    /// Unit tests for the <see cref="RestierHttpContextExtensions"/> class.
    /// </summary>
    public class RestierHttpContextExtensionsTests
    {
        private readonly RestierBatchChangeSetRequestItem restierBatchRequestItem;

        public RestierHttpContextExtensionsTests()
        {
            restierBatchRequestItem = new RestierBatchChangeSetRequestItem(
                new EmptyApi(Substitute.For<IEdmModel>(), Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>()),
                new[] { Substitute.For<HttpContext>() }
            );
        }

        [Fact]
        public void SetChangeSet_ShouldAddChangeSetToHttpContextItems()
        {
            // Arrange
            var context = Substitute.For<HttpContext>();
            var items = new System.Collections.Generic.Dictionary<object, object>();
            context.Items.Returns(items);

            var changeSetProperty = new RestierChangeSetProperty(restierBatchRequestItem);

            // Act
            context.SetChangeSet(changeSetProperty);

            // Assert
            Assert.True(items.ContainsKey("Microsoft.Restier.Submit.ChangeSet"));
            Assert.Equal(changeSetProperty, items["Microsoft.Restier.Submit.ChangeSet"]);
        }

        [Fact]
        public void GetChangeSet_ShouldReturnChangeSetFromHttpContextItems()
        {
            // Arrange
            var context = Substitute.For<HttpContext>();
            var items = new System.Collections.Generic.Dictionary<object, object>();
            var changeSetProperty = new RestierChangeSetProperty(restierBatchRequestItem);
            items["Microsoft.Restier.Submit.ChangeSet"] = changeSetProperty;
            context.Items.Returns(items);

            // Act
            var result = context.GetChangeSet();

            // Assert
            Assert.Equal(changeSetProperty, result);
        }

        [Fact]
        public void GetChangeSet_ShouldReturnNullIfChangeSetNotPresent()
        {
            // Arrange
            var context = Substitute.For<HttpContext>();
            var items = new System.Collections.Generic.Dictionary<object, object>();
            context.Items.Returns(items);

            // Act
            var result = context.GetChangeSet();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void SetChangeSet_ShouldThrowArgumentNullException_WhenContextIsNull()
        {
            // Arrange
            HttpContext context = null;
            var changeSetProperty = new RestierChangeSetProperty(restierBatchRequestItem);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => context.SetChangeSet(changeSetProperty));
        }

        [Fact]
        public void GetChangeSet_ShouldThrowArgumentNullException_WhenContextIsNull()
        {
            // Arrange
            HttpContext context = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => context.GetChangeSet());
        }

        public class EmptyApi : ApiBase
        {
            public EmptyApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }
        }
    }
}
