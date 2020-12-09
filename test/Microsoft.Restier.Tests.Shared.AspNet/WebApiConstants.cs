// <copyright file="WebApiConstants.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.AspNet
{
    /// <summary>
    /// A set of constants used by the Restier tests to simplify the configuration of test runs.
    /// </summary>
    /// <remarks>
    /// Since unit testing a WebApi should not require knowledge of a *specific* endpoint Url to execute (that's required in *integration* testing),
    /// these constants allow the test to run in a way that abstracts the details of configuring the API away from the developer. That allows the
    /// developer to focus on what is being tested, not on messing with configuration.
    /// </remarks>
    public static class WebApiConstants
    {
        /// <summary>
        /// Gets the default accept header.
        /// </summary>
        public const string DefaultAcceptHeader = "application/json";

        /// <summary>
        /// Gets the default host.
        /// </summary>
        public const string Localhost = "http://localhost/";

        /// <summary>
        /// Gets the default route prefix.
        /// </summary>
        public const string RoutePrefix = "api/test";

        /// <summary>
        /// The default name of the route for the ASP.NET route dictionary.
        /// </summary>
        public const string RouteName = "api/tests";
    }
}