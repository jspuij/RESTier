// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Restier.AspNetCore.Routing;

/// <summary>
/// Provides an application model for Restier APIs in ASP.NET Core.
/// </summary>
public class RestierApplicationModelProvider : IApplicationModelProvider
{
    /// <inheritdoc cref="IApplicationModelProvider"/>
    public int Order => throw new NotImplementedException();

    /// <inheritdoc cref="IApplicationModelProvider"/>
    public void OnProvidersExecuted(ApplicationModelProviderContext context)
    {
    }

    /// <inheritdoc cref="IApplicationModelProvider"/>
    public void OnProvidersExecuting(ApplicationModelProviderContext context)
    {
    }
}