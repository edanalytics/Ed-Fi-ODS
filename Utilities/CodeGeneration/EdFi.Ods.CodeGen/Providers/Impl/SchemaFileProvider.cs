// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.IO;
using EdFi.Common;
using EdFi.Ods.Common;
using log4net;

namespace EdFi.Ods.CodeGen.Providers.Impl
{
    public class SchemaFileProvider : ISchemaFileProvider
    {
        private const string EdFiSchemaName = "Ed-Fi-Core.xsd";
        private const string EdFiSchemaAnnotationName = "SchemaAnnotation.xsd";
        private const string ExtensionSchemaName = "EXTENSION-Ed-Fi-Extended-Core.xsd";

        private readonly ILog _logger = LogManager.GetLogger(typeof(SchemaFileProvider));

        private readonly IMetadataFolderProvider _metadataFolderProvider;

        public SchemaFileProvider(IMetadataFolderProvider metadataFolderProvider)
        {
            _metadataFolderProvider = metadataFolderProvider;
            Preconditions.ThrowIfNull(metadataFolderProvider, nameof(metadataFolderProvider));
        }

        public string GetEdFiSchema(string version)
        {
            string standardSchemaFolder = _metadataFolderProvider.GetStandardSchemaFolder(version);
            
            var file = Path.Combine(standardSchemaFolder, EdFiSchemaName);

            return File.Exists(file)
                ? file
                : throw new FileNotFoundException(GetExceptionMessage(standardSchemaFolder, EdFiSchemaName));
        }

        public string GetEdFiSchemaAnnotation(string version)
        {
            string standardSchemaFolder = _metadataFolderProvider.GetStandardSchemaFolder(version);
            
            var file = Path.Combine(standardSchemaFolder, EdFiSchemaAnnotationName);

            return File.Exists(file)
                ? file
                : throw new FileNotFoundException(GetExceptionMessage(standardSchemaFolder, EdFiSchemaAnnotationName));
        }

        public string GetExtensionSchema(string folder)
        {
            var file = Path.Combine(folder, ExtensionSchemaName);

            if (File.Exists(file))
            {
                return file;
            }

            // Extension schemas are optional
            _logger.Warn(GetExceptionMessage(folder, ExtensionSchemaName));
            return null;
        }

        private string GetExceptionMessage(string folder, string file)
            => $"Unable to find XSD file '{file}'.  Please make certain the XSD files exist in the {folder} folder.";
    }
}
