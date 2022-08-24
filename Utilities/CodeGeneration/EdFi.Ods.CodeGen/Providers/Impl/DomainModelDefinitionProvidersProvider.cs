// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EdFi.Ods.CodeGen.Conventions;
using EdFi.Ods.Common.Conventions;
using EdFi.Ods.Common.Models;
using log4net;

namespace EdFi.Ods.CodeGen.Providers.Impl
{
    public class DomainModelDefinitionProvidersProvider : IDomainModelDefinitionsProviderProvider
    {
        private static readonly string _standardModelsPath = Path.Combine("Artifacts", "Metadata", "ApiModel.json");
        private static readonly string _extensionModelsPath = Path.Combine("Artifacts", "Metadata", "ApiModel-EXTENSION.json");
        private readonly Lazy<Dictionary<VersionedPath, IDomainModelDefinitionsProvider>> _domainModelDefinitionProvidersByProjectName;

        private readonly string _solutionPath;
        private readonly ILog Logger = LogManager.GetLogger(typeof(DomainModelDefinitionProvidersProvider));
        private readonly IExtensionPluginsProvider _extensionPluginsProviderProvider;
        private readonly IIncludePluginsProvider _includePluginsProvider;
        private readonly string _extensionsPath;

        public DomainModelDefinitionProvidersProvider(
            ICodeRepositoryProvider codeRepositoryProvider,
            IExtensionPluginsProvider extensionPluginsProviderProvider,
            IIncludePluginsProvider includePluginsProvider)
        {
            _solutionPath = Path.Combine(codeRepositoryProvider.GetCodeRepositoryByName(CodeRepositoryConventions.Implementation), "Application");

            _extensionsPath = codeRepositoryProvider.GetResolvedCodeRepositoryByName(
                CodeRepositoryConventions.ExtensionsRepositoryName,
                "Extensions");

            _domainModelDefinitionProvidersByProjectName =
                new Lazy<Dictionary<VersionedPath, IDomainModelDefinitionsProvider>>(CreateDomainModelDefinitionsByPath);

            _extensionPluginsProviderProvider = extensionPluginsProviderProvider;

            _includePluginsProvider = includePluginsProvider;
        }

        /// <summary>
        /// Discover and instantiate all IDomainModelDefinitionsProviders in the solution
        /// Associate each provider with corresponding project type.
        /// </summary>
        /// <returns>An enumerable of IDomainModelDefinitionsProvider</returns>
        public IEnumerable<IDomainModelDefinitionsProvider> DomainModelDefinitionProviders()
        {
            return _domainModelDefinitionProvidersByProjectName.Value.Values;
        }

        public IDictionary<VersionedPath, IDomainModelDefinitionsProvider> DomainModelDefinitionsProvidersByProjectName()
        {
            return _domainModelDefinitionProvidersByProjectName.Value;
        }

        private Dictionary<VersionedPath, IDomainModelDefinitionsProvider> CreateDomainModelDefinitionsByPath()
        {
            DirectoryInfo[] directoriesToEvaluate;

            var domainModelDefinitionsByVersionedPath =
                new Dictionary<VersionedPath, IDomainModelDefinitionsProvider>();

            string edFiOdsImplementationApplicationPath = _solutionPath;

            string edFiOdsApplicationPath = _solutionPath.Replace(
                CodeRepositoryConventions.EdFiOdsImplementationFolderName,
                CodeRepositoryConventions.EdFiOdsFolderName);

            directoriesToEvaluate = GetProjectDirectoriesToEvaluate(edFiOdsImplementationApplicationPath)
                .Concat(GetProjectDirectoriesToEvaluate(edFiOdsApplicationPath))
                .ToArray();

            var extensionPaths = _extensionPluginsProviderProvider.GetExtensionLocationPlugins();

            extensionPaths.ToList().ForEach(
                x =>
                {
                    if (!Directory.Exists(x))
                    {
                        throw new Exception(
                            $"Unable to find extension Location project path  at location {x}.");
                    }

                    directoriesToEvaluate = directoriesToEvaluate
                        .Concat(GetProjectDirectoriesToEvaluate(x))
                        .Append(new DirectoryInfo(x)).ToArray();
                });

            if (_includePluginsProvider.IncludePlugins() && Directory.Exists(_extensionsPath))
            {
                directoriesToEvaluate = directoriesToEvaluate
                    .Concat(GetProjectDirectoriesToEvaluate(_extensionsPath))
                    .ToArray();
            }

            var modelProjects = directoriesToEvaluate
                .Where(p => p.Name.IsExtensionAssembly() || p.Name.IsStandardAssembly());

            foreach (var modelProject in modelProjects)
            {
                var modelsRelativePath = modelProject.Name.IsStandardAssembly()
                    ? _standardModelsPath
                    : _extensionModelsPath;

                var versionDirectories = Directory.GetDirectories(modelProject.FullName)
                    .Where(d => char.IsNumber(Path.GetFileName(d)[0]))
                    .ToArray();

                foreach (string versionDirectory in versionDirectories)
                {
                    var metadataFile = new FileInfo(Path.Combine(versionDirectory, modelsRelativePath));

                    Logger.Debug($"Loading ApiModels for {metadataFile}.");

                    if (!metadataFile.Exists)
                    {
                        throw new Exception(
                            $"Unable to find model definitions file for extensions project {modelProject.Name} at location {metadataFile.FullName}.");
                    }

                    string version = Path.GetFileName(versionDirectory);

                    if (domainModelDefinitionsByVersionedPath.ContainsKey(new VersionedPath(modelProject.Name, version)))
                    {
                        throw new Exception($"Cannot process duplicate extension projects for '{modelProject.Name}' and version '{version}'.");
                    }

                    domainModelDefinitionsByVersionedPath.Add(
                        new VersionedPath(modelProject.Name, version),
                        new DomainModelDefinitionsJsonFileSystemProvider(metadataFile.FullName));
                }
            }

            return domainModelDefinitionsByVersionedPath;

            DirectoryInfo[] GetProjectDirectoriesToEvaluate(string basePath)
            {
                var directory = new DirectoryInfo(basePath);

                if (directory.Exists)
                {
                    return directory.GetDirectories("", SearchOption.AllDirectories);
                }

                return new DirectoryInfo[0];
            }
        }
    }
}
