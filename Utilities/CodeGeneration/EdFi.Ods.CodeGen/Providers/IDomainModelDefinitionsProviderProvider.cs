// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using EdFi.Ods.CodeGen.Providers.Impl;
using EdFi.Ods.Common.Models;

namespace EdFi.Ods.CodeGen.Providers
{
    public interface IDomainModelDefinitionsProviderProvider
    {
        IEnumerable<IDomainModelDefinitionsProvider> DomainModelDefinitionProviders();

        IDictionary<VersionedPath, IDomainModelDefinitionsProvider> DomainModelDefinitionsProvidersByProjectName();
    }

    public struct VersionedPath
    {
        public VersionedPath(string name, string version, string edFiVersion)
        {
            Name = name;
            Version = version;
            EdFiVersion = edFiVersion;
        }

        public string Name { get; }
        public string Version { get; }
        
        /// <summary>
        /// Gets the Ed-Fi version in context for the extension model/version indicated by the <see cref="Name" /> and <see cref="Version" /> properties.
        /// </summary>
        public string EdFiVersion { get; }
    }
}
