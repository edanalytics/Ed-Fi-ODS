// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.Api.ExceptionHandling.Translators.Postgres;

// See https://www.postgresql.org/docs/current/errcodes-appendix.html

public static class PostgresSqlStates
{
    public const string ForeignKeyViolation = Npgsql.PostgresErrorCodes.ForeignKeyViolation;
    public const string UniqueViolation = Npgsql.PostgresErrorCodes.UniqueViolation;
}
