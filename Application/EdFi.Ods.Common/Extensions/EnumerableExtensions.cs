// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;

namespace EdFi.Ods.Common.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> ForEach<T, TArg>(this IEnumerable<T> source, Action<T, TArg> action, TArg arg)
        {
            foreach (var item in source)
            {
                action(item, arg);
            }

            return source;
        }

        public static IEnumerable<T> ForEach<T, TArg>(this IEnumerable<T> source, Action<T, int, TArg> action, TArg arg)
        {
            int i = 0;
            
            foreach (var item in source)
            {
                action(item, i++, arg);
            }

            return source;
        }
    }
}
