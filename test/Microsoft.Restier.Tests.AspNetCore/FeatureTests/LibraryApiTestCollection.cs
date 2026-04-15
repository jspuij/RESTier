// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

/// <summary>
/// Defines a test collection for all feature tests that share the LibraryApi in-memory database.
/// Tests within this collection run sequentially to avoid data contention.
/// </summary>
[CollectionDefinition("LibraryApi")]
public class LibraryApiTestCollection;
