// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EFCore.MultiTenancy;

internal interface IConnectionStringProvider
{
    string GetConnectionString(string tenantId);

    bool TryGetConnectionString(string tenantId, out string connectionString);
}
