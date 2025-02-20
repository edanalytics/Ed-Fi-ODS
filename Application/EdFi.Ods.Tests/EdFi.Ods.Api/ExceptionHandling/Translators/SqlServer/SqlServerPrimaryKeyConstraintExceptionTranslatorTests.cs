﻿// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Ods.Api.ExceptionHandling.Translators.SqlServer;
using EdFi.Ods.Api.Models;
using EdFi.Ods.Api.Security.Authentication;
using EdFi.Ods.Common.Context;
using EdFi.Ods.Common.Exceptions;
using EdFi.Ods.Common.Models.Resource;
using EdFi.Ods.Common.Security.Claims;
using EdFi.Ods.Tests._Builders;
using EdFi.Ods.Tests._Helpers;
using EdFi.TestFixture;
using NHibernate.Exceptions;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.Tests.EdFi.Ods.Api.ExceptionHandling.Translators.SqlServer
{
    public class SqlServerPrimaryKeyConstraintExceptionTranslatorTests
    {
        [TestFixture]
        public class When_a_regular_exception_is_thrown : TestFixtureBase
        {
            private Exception exception;
            private bool result;

            protected override void Arrange()
            {
                exception = new Exception();
            }

            protected override void Act()
            {
                var translator = new SqlServerPrimaryKeyConstraintExceptionTranslator(Stub<IContextProvider<DataManagementResourceContext>>());
                IEdFiProblemDetails actualError;
                result = translator.TryTranslate(exception, out actualError);
            }

            [Test]
            public void Should_not_handle_this_exception()
            {
                result.ShouldBeFalse();
            }
        }

        [TestFixture]
        public class When_a_generic_ADO_exception_is_thrown_without_an_inner_exception
            : TestFixtureBase
        {
            private Exception exception;
            private bool wasHandled;
            private IEdFiProblemDetails actualError;

            protected override void Arrange()
            {
                exception = new GenericADOException("Generic exception message", null);
            }

            protected override void Act()
            {
                var translator = new SqlServerPrimaryKeyConstraintExceptionTranslator(Stub<IContextProvider<DataManagementResourceContext>>());
                wasHandled = translator.TryTranslate(exception, out actualError);
            }

            [Test]
            public void Should_not_handle()
            {
                wasHandled.ShouldBeFalse();
            }
        }

        [TestFixture]
        public class When_a_generic_ADO_exception_is_thrown_with_an_inner_exception_with_the_wrong_message
            : TestFixtureBase
        {
            private Exception exception;
            private bool wasHandled;
            private IEdFiProblemDetails actualError;

            protected override void Arrange()
            {
                const string slightlyWrongMessage =
                    "VViolation of PRIMARY KEY constraint 'PK_Session'. Cannot insert duplicate key in object 'edfi.Session'. The duplicate key value is (900007, 9, 2014). The statement has been terminated.";

                exception = NHibernateExceptionBuilder.CreateException("Some generic SQL Exception message", slightlyWrongMessage);
            }

            protected override void Act()
            {
                var translator = new SqlServerPrimaryKeyConstraintExceptionTranslator(Stub<IContextProvider<DataManagementResourceContext>>());
                wasHandled = translator.TryTranslate(exception, out actualError);
            }

            [Test]
            public void Should_not_handle()
            {
                wasHandled.ShouldBeFalse();
            }
        }

        [TestFixture]
        public class When_an_nHibernate_ADO_exception_is_thrown_with_an_inner_SQL_exception_primary_key_violation_for_an_abstract_table : TestFixtureBase
        {
            private Exception exception;
            private bool result;
            private IEdFiProblemDetails actualError;
            private ContextProvider<DataManagementResourceContext> _contextProvider;

            protected override void Arrange()
            {
                var domainModel = this.LoadDomainModel("GeneralStudentProgramAssociation");
                var resourceModel = new ResourceModel(domainModel);
                var resource = resourceModel.GetResourceByApiCollectionName("ed-fi", "studentProgramAssociations");
                _contextProvider = new ContextProvider<DataManagementResourceContext>(new HashtableContextStorage());
                _contextProvider.Set(new DataManagementResourceContext(resource));

                string mess =
                    $"Violation of PRIMARY KEY constraint 'GeneralStudentProgramAssociation_PK'. Cannot insert duplicate key in object 'edfi.GeneralStudentProgramAssociation'. The duplicate key value is (2021-08-30, 255901, 255901, Career and Technical Education, 1921, 1). {Environment.NewLine}The statement has been terminated.";

                exception = NHibernateExceptionBuilder.CreateException("Generic SQL Exception message...", mess);
            }

            protected override void Act()
            {
                var translator = new SqlServerPrimaryKeyConstraintExceptionTranslator(_contextProvider);
                result = translator.TryTranslate(exception, out actualError);
            }

            [Test]
            public void Should_handle_this_exception()
            {
                result.ShouldBeTrue();
            }

            [Test]
            public void Should_set_a_reasonable_message()
            {
                actualError.Detail.ShouldBe(
                    "The identifying value(s) of the item are the same as another item that already exists.");
                
                actualError.Errors.Length.ShouldBe(1);
                actualError.Errors.Single().ShouldBe(
                    "A primary key conflict occurred when attempting to create or update a record in the 'StudentProgramAssociation' table. The duplicate key is (BeginDate, EducationOrganizationId, ProgramEducationOrganizationId, ProgramName, ProgramTypeDescriptorId, StudentUSI) = (2021-08-30, 255901, 255901, Career and Technical Education, 1921, 1).");
            }

            [Test]
            public void Should_set_the_exception_type_to_conflict()
            {
                actualError.Type.ShouldBe(string.Join(':', EdFiProblemDetailsExceptionBase.BaseTypePrefix, "conflict:non-unique-identity"));
            }

            [Test]
            public void Should_translate_the_exception_to_a_409_error()
            {
                actualError.Status.ShouldBe(409);
            }
        }

        [TestFixture]
        public class When_an_nHibernate_ADO_exception_is_thrown_with_an_inner_SQL_exception_primary_key_violation : TestFixtureBase
        {
            private Exception exception;
            private bool result;
            private IEdFiProblemDetails actualError;
            private ContextProvider<DataManagementResourceContext> _contextProvider;

            protected override void Arrange()
            {
                var domainModel = this.LoadDomainModel("GeneralStudentProgramAssociation");
                var resourceModel = new ResourceModel(domainModel);
                var resource = resourceModel.GetResourceByApiCollectionName("ed-fi", "sessions");
                _contextProvider = new ContextProvider<DataManagementResourceContext>(new HashtableContextStorage());
                _contextProvider.Set(new DataManagementResourceContext(resource));

                string mess =
                    $"Violation of PRIMARY KEY constraint 'PK_Session'. Cannot insert duplicate key in object 'edfi.Session'. The duplicate key value is (900007, 9, 2014). {Environment.NewLine}The statement has been terminated.";

                exception = NHibernateExceptionBuilder.CreateException("Generic SQL Exception message...", mess);
            }

            protected override void Act()
            {
                var translator = new SqlServerPrimaryKeyConstraintExceptionTranslator(_contextProvider);
                result = translator.TryTranslate(exception, out actualError);
            }

            [Test]
            public void Should_handle_this_exception()
            {
                result.ShouldBeTrue();
            }

            [Test]
            public void Should_set_a_reasonable_message()
            {
                actualError.Detail.ShouldBe(
                    "The identifying value(s) of the item are the same as another item that already exists.");
                
                actualError.Errors.Length.ShouldBe(1);
                actualError.Errors.Single().ShouldBe(
                    "A primary key conflict occurred when attempting to create or update a record in the 'Session' table. The duplicate key is (SchoolId, SchoolYear, SessionName) = (900007, 9, 2014).");
            }

            [Test]
            public void Should_set_the_exception_type_to_conflict()
            {
                actualError.Type.ShouldBe(string.Join(':', EdFiProblemDetailsExceptionBase.BaseTypePrefix, "conflict:non-unique-identity"));
            }

            [Test]
            public void Should_translate_the_exception_to_a_409_error()
            {
                actualError.Status.ShouldBe(409);
            }
        }

        [TestFixture]
        public class When_an_nHibernate_ADO_exception_is_thrown_with_an_inner_SQL_exception_primary_key_violation_and_a_backwards_PK_name
            : TestFixtureBase
        {
            private Exception exception;
            private bool result;
            private IEdFiProblemDetails actualError;
            private ContextProvider<DataManagementResourceContext> _contextProvider;

            protected override void Arrange()
            {
                var domainModel = this.LoadDomainModel("GeneralStudentProgramAssociation");
                var resourceModel = new ResourceModel(domainModel);
                var resource = resourceModel.GetResourceByApiCollectionName("ed-fi", "sessions");
                _contextProvider = new ContextProvider<DataManagementResourceContext>(new HashtableContextStorage());
                _contextProvider.Set(new DataManagementResourceContext(resource));

                var mess =
                    $"Violation of PRIMARY KEY constraint 'BackwardsPkName_PK'. Cannot insert duplicate key in object 'edfi.Session'. The duplicate key value is (900007, 9, 2014). {Environment.NewLine}The statement has been terminated.";

                exception = NHibernateExceptionBuilder.CreateException("Generic exception message", mess);
            }

            protected override void Act()
            {
                var translator = new SqlServerPrimaryKeyConstraintExceptionTranslator(_contextProvider);
                result = translator.TryTranslate(exception, out actualError);
            }

            [Test]
            public void Should_handle_this_exception()
            {
                result.ShouldBeTrue();
            }
        }

        [TestFixture]
        public class When_an_nHibernate_ADO_exception_is_thrown_with_an_inner_exception_of_the_wrong_type : TestFixtureBase
        {
            private Exception exception;
            private bool result;
            private IEdFiProblemDetails actualError;

            protected override void Arrange()
            {
                var mess =
                    "Violation of PRIMARY KEY constraint 'PK_Session'. Cannot insert duplicate key in object 'edfi.Session'. The duplicate key value is (900007, 9, 2014). The statement has been terminated.";

                var innerexception = new Exception(mess);
                exception = new GenericADOException("Generic exception message", innerexception);
            }

            protected override void Act()
            {
                var translator = new SqlServerPrimaryKeyConstraintExceptionTranslator(Stub<IContextProvider<DataManagementResourceContext>>());
                result = translator.TryTranslate(exception, out actualError);
            }

            [Test]
            public void Should_not_handle_this_exception()
            {
                result.ShouldBeFalse();
            }
        }
    }
}