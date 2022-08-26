// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using EdFi.Common;
using EdFi.Common.Extensions;
using EdFi.Ods.CodeGen.Models;
using EdFi.Ods.CodeGen.Providers;
using EdFi.Ods.Common.Conventions;
using EdFi.Ods.Common.Models;
using EdFi.Ods.Common.Models.Domain;

namespace EdFi.Ods.CodeGen.Processing.Impl
{
    public class TemplateContextProvider : ITemplateContextProvider
    {
        private readonly Lazy<List<IDomainModelDefinitionsProvider>> _domainModelDefinitionProviders;
        private readonly IDomainModelDefinitionsProviderProvider _domainModelDefinitionsProviderProvider;
        
        private readonly ConcurrentDictionary<string, TemplateContext> _templateContextByEdFiStandardVersion = new();
        private readonly ConcurrentDictionary<(string, string, string), TemplateContext> _templateContextByAssemblyName = new();

        private readonly IEnumerable<AssemblyData> _edFiStandardAssemblyDatum;

        public TemplateContextProvider(
            IDomainModelDefinitionsProviderProvider domainModelDefinitionsProviderProvider,
            IEnumerable<IAssemblyDataProvider> assemblyDataProviders)
        {
            _domainModelDefinitionsProviderProvider = Preconditions.ThrowIfNull(
                domainModelDefinitionsProviderProvider,
                nameof(domainModelDefinitionsProviderProvider));

            _domainModelDefinitionProviders = new Lazy<List<IDomainModelDefinitionsProvider>>(
                () => _domainModelDefinitionsProviderProvider.DomainModelDefinitionProviders().ToList());

            // Get all the Ed-Fi Standard assemblies
            _edFiStandardAssemblyDatum = assemblyDataProviders.SelectMany(p => p.Get()).Where(ad => ad.IsStandard);
        }

        public IEnumerable<TemplateContext> Create(AssemblyData assemblyData)
        {
            Preconditions.ThrowIfNull(assemblyData, nameof(assemblyData));

            if (assemblyData.IsStandard)
            {
                // If this is an Ed-Fi Standard assembly, just return a single template context
                yield return _templateContextByEdFiStandardVersion.GetOrAdd(
                    assemblyData.ModelVersion,
                    k => GetTemplateContext(assemblyData));
            }
            else
            {
                // For non-Ed-Fi standard assemblies, create template contexts using permutation with all available Ed-Fi Standards
                foreach (var edFiStandardAssemblyData in _edFiStandardAssemblyDatum)
                {
                    yield return _templateContextByAssemblyName.GetOrAdd(
                        (assemblyData.AssemblyName, assemblyData.ModelVersion, edFiStandardAssemblyData.ModelVersion),
                        k => GetTemplateContext(assemblyData, edFiStandardAssemblyData));
                }
            }
        }

        private TemplateContext GetTemplateContext(AssemblyData assemblyData, AssemblyData edFiStandardAssemblyData = null)
        {
            var domainModelProvider = CreateDomainModelProvider(assemblyData, edFiStandardAssemblyData);

            var schemaNameMap = domainModelProvider.GetDomainModel()
                .SchemaNameMapProvider.GetSchemaMapByProperCaseName(assemblyData.SchemaName);

            return new TemplateContext
            {
                ProjectPath = assemblyData.Path,
                IsProfiles = assemblyData.IsProfile,
                IsExtension = assemblyData.IsExtension,
                ModelVersion = assemblyData.ModelVersion,
                EdFiStandardVersion = edFiStandardAssemblyData?.ModelVersion,
                SchemaProperCaseName = schemaNameMap.ProperCaseName,
                DomainModelProvider = domainModelProvider,
                SchemaPhysicalName = schemaNameMap.PhysicalName
            };
        }

        private IDomainModelProvider CreateDomainModelProvider(AssemblyData assemblyData, AssemblyData edFiStandardAssemblyData = null)
        {
            List<IDomainModelDefinitionsProvider> domainModelDefinitionsToLoad = null;

            // Profiles needs everything to be loaded
            if (assemblyData.IsProfile)
            {
                throw new Exception(
                    "Profiles must be generated with \"all available models\" loaded, but with multi-version support, this context becomes insufficiently defined -- needs further analysis on how to configure domain model provider in this sort of context.");

                // return new DomainModelProvider(
                //     _domainModelDefinitionProviders.Value,
                //     Array.Empty<IDomainModelDefinitionsTransformer>());
            }

            // Include EdFi, and only the extension definition provider that matches the current context
            // (This avoids any potential issues with templates not correctly handling multiple extension models active)
            var domainModelDefinitionsProviders = GetDomainModelDefinitionProviders(assemblyData, edFiStandardAssemblyData);

            return new DomainModelProvider(
                domainModelDefinitionsProviders,
                Array.Empty<IDomainModelDefinitionsTransformer>());
        }

        private List<IDomainModelDefinitionsProvider> GetDomainModelDefinitionProviders(AssemblyData assemblyData, AssemblyData edFiStandardAssemblyData)
        {
            string schemaName = assemblyData.SchemaName;
            string modelVersion = assemblyData.ModelVersion;

            // SPIKE: This extra argument might be redundant with the addition of EdFiVersion to the assemblyData itself.
            // In the end it probably is ideal if the generated API model has the associated Ed-Fi version included as a property (configuration)
            // vs. it being inferred from the folder structure (convention). 
            string edFiStandardVersion = edFiStandardAssemblyData.ModelVersion;
            
            return _domainModelDefinitionProviders.Value.Where(
                    x =>
                    {
                        var schemaDefinition = x.GetDomainModelDefinitions().SchemaDefinition;
                        string properCaseName = ExtensionsConventions.GetProperCaseNameForLogicalName(schemaDefinition.LogicalName);

                        return

                            // Domain Model Definitions are for the specified Ed-Fi Standard version
                            (schemaDefinition.LogicalName.EqualsIgnoreCase(EdFiConventions.LogicalName)
                                && schemaDefinition.Version.EqualsIgnoreCase(edFiStandardVersion)

                            // Domain Model Definitions are for the extension schema
                            || (schemaName.EqualsIgnoreCase(properCaseName) && schemaDefinition.Version.Equals(modelVersion) && ));
                    })
                .ToList();
        }
    }
}
