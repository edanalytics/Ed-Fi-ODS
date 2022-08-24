// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using EdFi.Common;
using EdFi.Ods.CodeGen.Models;

namespace EdFi.Ods.CodeGen.Providers.Impl
{
    public class JsonViewsProvider : JsonFileProvider, IAuthorizationDatabaseTableViewsProvider
    {
        private readonly ConcurrentDictionary<string, List<AuthorizationDatabaseTable>> _viewsByModelVersion = new();
        private readonly IMetadataFolderProvider _metadataFolderProvider;

        public JsonViewsProvider(IMetadataFolderProvider metadataFolderProvider)
        {
            _metadataFolderProvider = metadataFolderProvider;
            Preconditions.ThrowIfNull(metadataFolderProvider, nameof(metadataFolderProvider));
        }

        public List<AuthorizationDatabaseTable> LoadViews(string templateContextModelVersion)
        {
            return _viewsByModelVersion.GetOrAdd(
                templateContextModelVersion,
                mv => Load<List<AuthorizationDatabaseTable>>(
                    Path.Combine(
                        _metadataFolderProvider.GetStandardMetadataFolder(templateContextModelVersion),
                        "DatabaseViews.generated.json")));
        }
    }
}
