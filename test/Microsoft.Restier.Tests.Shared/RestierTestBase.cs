// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using System.Diagnostics;
using Xunit;

namespace Microsoft.Restier.Tests.Shared
{

    /// <summary>
    /// 
    /// </summary>
    public class RestierTestBase<TApi>: RestierBreakdanceTestBase<TApi>
        where TApi : ApiBase
    {
        public RestierTestBase()
        {
            Trace.Listeners.Add(TraceListener);
        }
        /// <summary>
        /// Gets the XUnit test context.
        /// </summary>
        public ITestContext TestContext => Xunit.TestContext.Current;

        /// <summary>
        /// Gets the Trace Listener that can be used for test output.
        /// </summary>
        public TraceListener TraceListener { get; } = new TestTraceListener();

    }

}