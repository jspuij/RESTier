// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.AspNetCore.Routing;

/// <summary>
/// Sentinel class registered in per-route DI services to identify Restier routes.
/// Used to distinguish Restier routes from other OData routes when creating dynamic catch-all endpoints.
/// </summary>
internal sealed class RestierRouteMarker
{
}
