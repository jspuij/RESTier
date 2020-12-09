// <copyright file="ChainedService.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core
{
    using System;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// A service inside the DI container that is chained to another service in the DI contianer.
    /// </summary>
    /// <typeparam name="TService">The Service type.</typeparam>
    internal static class ChainedService<TService>
        where TService : class
    {
        /// <summary>
        /// A factory that will create the chained services.
        /// </summary>
        public static readonly Func<IServiceProvider, TService> DefaultFactory = sp =>
        {
            // get all instances in reverse order.
            var instances = sp.GetServices<ApiServiceContributor<TService>>().Reverse();

            using (var enumerator = instances.GetEnumerator())
            {
                // use a recursive function to chain the services.
                TService Next()
                {
                    if (enumerator.MoveNext())
                    {
                        return enumerator.Current(sp, Next);
                    }

                    return null;
                }

                return Next();
            }
        };
    }
}
