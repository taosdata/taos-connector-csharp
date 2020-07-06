// Copyright (c)  Maikebing. All rights reserved.
// Licensed under the MIT License, See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Maikebing.EntityFrameworkCore.Taos.Query.Internal
{
    public class TaosStringLengthTranslator : IMemberTranslator
    {
        private readonly ISqlExpressionFactory _sqlExpressionFactory;

        public TaosStringLengthTranslator(ISqlExpressionFactory sqlExpressionFactory)
        {
            _sqlExpressionFactory = sqlExpressionFactory;
        }

        public virtual SqlExpression Translate(SqlExpression instance, MemberInfo member, Type returnType)
        {
            return instance?.Type == typeof(string)
                && member.Name == nameof(string.Length)
                    ? _sqlExpressionFactory.Function("length", new[] { instance }, returnType)
                    : null;
        }
    }
}
