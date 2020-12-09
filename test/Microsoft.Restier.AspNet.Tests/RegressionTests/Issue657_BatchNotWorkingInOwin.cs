// <copyright file="Issue657_BatchNotWorkingInOwin.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.AspNet.Tests.RegressionTests
{
    using System;
    using System.Web.Http;
    using FluentAssertions;
    using Microsoft.Restier.Tests.Shared.AspNet.Scenarios.Library;
    using Xunit;

    /// <summary>
    /// Regression tests for https://github.com/OData/RESTier/issues/657.
    /// </summary>
    public class Issue657_BatchNotWorkingInOwin
    {
        /// <summary>
        /// Checks that maprestier throws an exception on selfhost.
        /// </summary>
        [Fact(Skip= "RWM: Need a way to make this test work.")]
        public void MapRestier_ThrowsExceptionOnOwinSelfHost()
        {
            var config = new HttpConfiguration();
            Action mapRestier = () => { config.MapRestier<LibraryApi>("Restier", "v1/"); };
            mapRestier.Should().Throw<Exception>().WithMessage("*MapRestier*");
        }

        /// <summary>
        /// Checks that maprestier throws on a null HttpServer.
        /// </summary>
        [Fact]
        public void MapRestier_ThrowsExceptionOnNullHttpServer()
        {
            var config = new HttpConfiguration();
            Action mapRestier = () => { config.MapRestier<LibraryApi>("Restier", "v1/", true, null); };
            mapRestier.Should().Throw<Exception>().WithMessage("*MapRestier*");
        }
    }
}