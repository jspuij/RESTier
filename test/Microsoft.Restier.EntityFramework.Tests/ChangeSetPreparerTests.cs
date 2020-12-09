// <copyright file="ChangeSetPreparerTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.EntityFramework.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Submit;
    using Microsoft.Restier.Tests.Shared.AspNet;
    using Microsoft.Restier.Tests.Shared.AspNet.Scenarios.Library;
    using Microsoft.Restier.Tests.Shared.EntityFramework.Scenarios.Library;
    using Microsoft.Restier.Tests.Shared.Extensions;
    using Microsoft.Restier.Tests.Shared.Scenarios.Library;
    using Xunit;

    /// <summary>
    /// EFChangeSetPreparer tests.
    /// </summary>
    public class ChangeSetPreparerTests
    {
        /// <summary>
        /// Tests the update of a complex type.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ComplexTypeUpdate()
        {
            // Arrange
            var provider = await RestierTestHelpers.GetTestableInjectionContainer<LibraryApi, LibraryContext>();
            var api = provider.GetTestableApiInstance<LibraryApi>();

            var item = new DataModificationItem(
                "Readers",
                typeof(Employee),
                null,
                RestierEntitySetOperation.Update,
                new Dictionary<string, object> { { "Id", new Guid("53162782-EA1B-4712-AF26-8AA1D2AC0461") } },
                new Dictionary<string, object>(),
                new Dictionary<string, object> { { "Addr", new Dictionary<string, object> { { "Zip", "332" } } } });
            var changeSet = new ChangeSet(new[] { item });
            var sc = new SubmitContext(api, changeSet);

            // Act
            var changeSetPreparer = api.GetApiService<IChangeSetInitializer>();
            await changeSetPreparer.InitializeAsync(sc, CancellationToken.None).ConfigureAwait(false);
            var person = item.Resource as Employee;

            // Assert
            person.Should().NotBeNull();
            person.Addr.Zip.Should().Be("332");
        }
    }
}
