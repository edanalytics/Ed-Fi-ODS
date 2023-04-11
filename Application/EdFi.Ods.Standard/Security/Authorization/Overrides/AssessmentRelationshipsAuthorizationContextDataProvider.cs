// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Ods.Api.Security.AuthorizationStrategies.Relationships;
using EdFi.Ods.Entities.Common.EdFi;
using EdFi.Ods.Entities.NHibernate.AssessmentAggregate.EdFi;

namespace EdFi.Ods.Standard.Security.Authorization.Overrides;

public class AssessmentRelationshipsAuthorizationContextDataProvider<TContextData> : IRelationshipsAuthorizationContextDataProvider<IAssessment, TContextData>
    where TContextData : RelationshipsAuthorizationContextData, new()
{
    /// <summary>
    /// Creates and returns an <see cref="TContextData"/> instance based on the supplied resource.
    /// </summary>
    public TContextData GetContextData(IAssessment resource)
    {
        if (resource == null)
            throw new ArgumentNullException("resource", "The 'assessment' resource for obtaining authorization context data cannot be null.");

        var entity = resource as Assessment;

        var contextData = new TContextData();
        contextData.EducationOrganizationId = entity?.EducationOrganizationId;
        return contextData;
    }

    /// <summary>
    ///  Creates and returns a signature key based on the resource, which can then be used to get and instance of IEdFiSignatureAuthorizationProvider
    /// </summary>
    public string[] GetAuthorizationContextPropertyNames()
    {
        var properties = new string[]
        {
            "EducationOrganizationId",
        };

        return properties;
    }

    /// <summary>
    /// Creates and returns an <see cref="RelationshipsAuthorizationContextData"/> instance based on the supplied resource.
    /// </summary>
    public TContextData GetContextData(object resource)
    {
        return GetContextData((Assessment) resource);
    }
}
