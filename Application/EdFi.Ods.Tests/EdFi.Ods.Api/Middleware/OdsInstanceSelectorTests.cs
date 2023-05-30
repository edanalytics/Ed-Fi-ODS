// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Ods.Api.Configuration;
using EdFi.Ods.Common.Configuration;
using EdFi.Ods.Common.Exceptions;
using EdFi.Ods.Common.Security;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.Api.Middleware.Tests
{
    // NOTE: Initial test fixture generated using ChatGPT
    public class OdsInstanceSelectorTests
    {
        private IApiClientContextProvider _apiClientContextProvider;
        private IOdsInstanceConfigurationProvider _odsInstanceConfigurationProvider;
        private OdsInstanceSelector _odsInstanceSelector;
        private Dictionary<string, object> _routeValueDictionary;

        [SetUp]
        public void SetUp()
        {
            _apiClientContextProvider = A.Fake<IApiClientContextProvider>();
            _odsInstanceConfigurationProvider = A.Fake<IOdsInstanceConfigurationProvider>();
            _odsInstanceSelector = new OdsInstanceSelector(_apiClientContextProvider, _odsInstanceConfigurationProvider);
            _routeValueDictionary = new Dictionary<string, object>();
        }

        [Test]
        public async Task GetOdsInstanceAsync_ReturnsNull_WhenApiClientContextIsNull()
        {
            // Arrange
            A.CallTo(() => _apiClientContextProvider.GetApiClientContext()).Returns(null);

            // Act
            var result = await _odsInstanceSelector.GetOdsInstanceAsync(_routeValueDictionary);

            // Assert
            result.ShouldBeNull();
        }

        [Test]
        public void GetOdsInstanceAsync_ThrowsException_WhenApiClientContextHasNoOdsInstanceIds()
        {
            // Arrange
            var apiClientContext = CreateApiClientContext();
            A.CallTo(() => _apiClientContextProvider.GetApiClientContext()).Returns(apiClientContext);

            // Act + Assert
            Assert.ThrowsAsync<ApiSecurityConfigurationException>(() => _odsInstanceSelector.GetOdsInstanceAsync(_routeValueDictionary));
        }

        [Test]
        public async Task GetOdsInstanceAsync_ReturnsOdsInstanceConfiguration_WhenApiClientContextHasOneOdsInstanceId()
        {
            // Arrange
            var odsInstanceId = 1;
            
            var apiClientContext = CreateApiClientContext(odsInstanceId);

            var odsInstanceConfiguration = new OdsInstanceConfiguration(
                odsInstanceId,
                (ulong)odsInstanceId,
                "TheConnectionString",
                new Dictionary<string, string>(),
                new Dictionary<DerivativeType, string>());
            
            A.CallTo(() => _apiClientContextProvider.GetApiClientContext()).Returns(apiClientContext);
            A.CallTo(() => _odsInstanceConfigurationProvider.GetByIdAsync(odsInstanceId)).Returns(odsInstanceConfiguration);

            // Act
            var result = await _odsInstanceSelector.GetOdsInstanceAsync(_routeValueDictionary);

            // Assert
            result.ShouldBe(odsInstanceConfiguration);
        }

        [Test]
        public async Task GetOdsInstanceAsync_ReturnsOdsInstanceConfiguration_WhenApiClientContextHasMoreThanOneOdsInstanceId_AndOneMatchingRouteKeyAndValue()
        {
            // Arrange
            var odsInstanceIds = new[] { 1, 2 };

            var apiClientContext = CreateApiClientContext(odsInstanceIds);

            var odsInstanceConfiguration_1 = new OdsInstanceConfiguration(
                1,
                1UL,
                "TheConnectionString",
                new Dictionary<string, string> { { "schoolYear", "2022" } },
                new Dictionary<DerivativeType, string>());

            var odsInstanceConfiguration_2 = new OdsInstanceConfiguration(
                2,
                2UL,
                "TheConnectionString",
                new Dictionary<string, string> { { "schoolYear", "2023"} },
                new Dictionary<DerivativeType, string>());

            A.CallTo(() => _apiClientContextProvider.GetApiClientContext()).Returns(apiClientContext);
            A.CallTo(() => _odsInstanceConfigurationProvider.GetByIdAsync(1)).Returns(odsInstanceConfiguration_1);
            A.CallTo(() => _odsInstanceConfigurationProvider.GetByIdAsync(2)).Returns(odsInstanceConfiguration_2);

            _routeValueDictionary.Add("schoolYear", "2023");

            // Act
            var result = await _odsInstanceSelector.GetOdsInstanceAsync(_routeValueDictionary);

            // Assert
            result.ShouldBe(odsInstanceConfiguration_2);
        }

        [Test]
        public async Task GetOdsInstanceAsync_ReturnsNull_WhenApiClientContextHasMultipleOdsInstanceIds_AndNoMatchingRouteKeyAndValue()
        {
            // Arrange
            var odsInstanceIds = new[] { 1, 2 };

            var apiClientContext = CreateApiClientContext(odsInstanceIds);

            var odsInstanceConfiguration_1 = new OdsInstanceConfiguration(
                1,
                1UL,
                "TheConnectionString",
                new Dictionary<string, string> { { "schoolYear", "2022" } },
                new Dictionary<DerivativeType, string>());

            var odsInstanceConfiguration_2 = new OdsInstanceConfiguration(
                2,
                2UL,
                "TheConnectionString",
                new Dictionary<string, string> { { "schoolYear", "2023" } },
                new Dictionary<DerivativeType, string>());

            A.CallTo(() => _apiClientContextProvider.GetApiClientContext()).Returns(apiClientContext);
            A.CallTo(() => _odsInstanceConfigurationProvider.GetByIdAsync(1)).Returns(odsInstanceConfiguration_1);
            A.CallTo(() => _odsInstanceConfigurationProvider.GetByIdAsync(2)).Returns(odsInstanceConfiguration_2);

            _routeValueDictionary.Add("schoolYear", "2024");

            // Act 
            var result = await _odsInstanceSelector.GetOdsInstanceAsync(_routeValueDictionary);

            // Assert
            result.ShouldBeNull();
        }

        private static ApiClientContext CreateApiClientContext(params int[] odsInstanceIds)
        {
            return new ApiClientContext(
                odsInstanceIds: odsInstanceIds,
                apiKey: "abc",
                claimSetName: "TestClaimSet",
                educationOrganizationIds: new [] { 1 },
                namespacePrefixes: Array.Empty<string>(),
                profiles: Array.Empty<string>(),
                studentIdentificationSystemDescriptor: null,
                creatorOwnershipTokenId: null,
                ownershipTokenIds: Array.Empty<short>(),
                apiClientId: 123);
        }
    }
}
