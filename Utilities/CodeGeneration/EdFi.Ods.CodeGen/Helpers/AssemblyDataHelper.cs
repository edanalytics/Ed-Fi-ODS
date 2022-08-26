// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EdFi.Common;
using EdFi.Common.Extensions;
using EdFi.Ods.CodeGen.Conventions;
using EdFi.Ods.CodeGen.Models;
using EdFi.Ods.CodeGen.Providers;
using EdFi.Ods.Common.Conventions;
using EdFi.Ods.Common.Models;

namespace EdFi.Ods.CodeGen.Helpers
{
    public class AssemblyDataHelper
    {
        private readonly IJsonFileProvider _jsonFileProvider;
        private readonly IDictionary<VersionedPath, IDomainModelDefinitionsProvider> _domainModelsDefinitionsProvidersByProjectName;

        public AssemblyDataHelper(
            IJsonFileProvider jsonFileProvider,
            IDomainModelDefinitionsProviderProvider domainModelDefinitionsProviderProvider)
        {
            _jsonFileProvider = Preconditions.ThrowIfNull(jsonFileProvider, nameof(jsonFileProvider));

            Preconditions.ThrowIfNull(domainModelDefinitionsProviderProvider, nameof(domainModelDefinitionsProviderProvider));

            _domainModelsDefinitionsProvidersByProjectName =
                domainModelDefinitionsProviderProvider.DomainModelDefinitionsProvidersByProjectName();
        }

        // last element is the assemblyName.
        public VersionedPath GetAssemblyName(string assemblyMetadataPath)
        {
            var parts = Path.GetDirectoryName(assemblyMetadataPath)
                ?.Split(Path.DirectorySeparatorChar)
                .Reverse()
                .Take(3)
                .ToArray();

            // SPIKE: Does this match an extension / Ed-Fi permutation?
            // No support in Spike for Profiles
            var edFiVersionForExtensionMatch = Regex.Match(parts[0], "^Ed-Fi-([0-9][^\\/]*)$");
            
            if (edFiVersionForExtensionMatch.Success)
            {
                return new VersionedPath(parts[2], parts[1], edFiVersionForExtensionMatch.Groups[1].Value);
            }

            return new VersionedPath(parts[1], parts[0], null);
        }

        public AssemblyData CreateAssemblyData(string assemblyMetadataPath)
        {
            var assemblyMetadata = _jsonFileProvider.Load<AssemblyMetadata>(assemblyMetadataPath);

            bool isExtension = assemblyMetadata.AssemblyModelType.EqualsIgnoreCase(TemplateSetConventions.Extension);

            var versionedAssemblyName = GetAssemblyName(assemblyMetadataPath);

            var schemaName = isExtension
                ? ExtensionsConventions.GetProperCaseNameForLogicalName(
                    _domainModelsDefinitionsProvidersByProjectName[versionedAssemblyName]
                        .GetDomainModelDefinitions()
                        .SchemaDefinition.LogicalName)
                : EdFiConventions.ProperCaseName;

            var assemblyData = new AssemblyData
            {
                AssemblyName = versionedAssemblyName.Name,
                ModelVersion = versionedAssemblyName.Version,
                EdFiVersion = versionedAssemblyName.EdFiVersion,
                Path = Path.GetDirectoryName(assemblyMetadataPath),
                TemplateSet = assemblyMetadata.AssemblyModelType,
                IsProfile = assemblyMetadata.AssemblyModelType.EqualsIgnoreCase(TemplateSetConventions.Profile),
                SchemaName = schemaName,
                IsExtension = isExtension
            };

            return assemblyData;
        }
    }
}
