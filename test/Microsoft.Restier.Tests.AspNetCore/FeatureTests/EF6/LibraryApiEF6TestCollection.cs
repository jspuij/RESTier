// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EF6;

/// <summary>
/// Defines a test collection for EF6 feature tests that share the LibraryApi database.
/// Tests within this collection run sequentially to avoid data contention.
/// </summary>
[CollectionDefinition("LibraryApiEF6")]
public class LibraryApiEF6TestCollection;
