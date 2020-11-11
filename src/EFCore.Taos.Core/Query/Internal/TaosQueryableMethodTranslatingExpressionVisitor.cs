// Copyright (c)  Maikebing. All rights reserved.
// Licensed under the MIT License, See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Maikebing.EntityFrameworkCore.Taos.Internal;

namespace Maikebing.EntityFrameworkCore.Taos.Query.Internal
{
    public class TaosQueryableMethodTranslatingExpressionVisitor : RelationalQueryableMethodTranslatingExpressionVisitor
    {
        public TaosQueryableMethodTranslatingExpressionVisitor(
            QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
            RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies,
            QueryCompilationContext queryCompilationContext)
            : base(dependencies, relationalDependencies, queryCompilationContext)
        {
        }

        protected TaosQueryableMethodTranslatingExpressionVisitor(
            TaosQueryableMethodTranslatingExpressionVisitor parentVisitor)
            : base(parentVisitor)
        {
        }

        protected override QueryableMethodTranslatingExpressionVisitor CreateSubqueryVisitor()
            => new TaosQueryableMethodTranslatingExpressionVisitor(this);

        protected override ShapedQueryExpression TranslateOrderBy(
            ShapedQueryExpression source, LambdaExpression keySelector, bool ascending)
        {
            var translation = base.TranslateOrderBy(source, keySelector, ascending);
            if (translation == null)
            {
                return null;
            }

            var orderingExpression = ((SelectExpression)translation.QueryExpression).Orderings.Last();
            var orderingExpressionType = GetProviderType(orderingExpression.Expression);
            if (orderingExpressionType == typeof(DateTimeOffset)
                || orderingExpressionType == typeof(decimal)
                || orderingExpressionType == typeof(TimeSpan)
                || orderingExpressionType == typeof(ulong))
            {
                throw new NotSupportedException(
                    TaosStrings.OrderByNotSupported(orderingExpressionType.ShortDisplayName()));
            }

            return translation;
        }

        protected override ShapedQueryExpression TranslateThenBy(ShapedQueryExpression source, LambdaExpression keySelector, bool ascending)
        {
            var translation = base.TranslateThenBy(source, keySelector, ascending);
            if (translation == null)
            {
                return null;
            }

            var orderingExpression = ((SelectExpression)translation.QueryExpression).Orderings.Last();
            var orderingExpressionType = GetProviderType(orderingExpression.Expression);
            if (orderingExpressionType == typeof(DateTimeOffset)
                || orderingExpressionType == typeof(decimal)
                || orderingExpressionType == typeof(TimeSpan)
                || orderingExpressionType == typeof(ulong))
            {
                throw new NotSupportedException(
                    TaosStrings.OrderByNotSupported(orderingExpressionType.ShortDisplayName()));
            }

            return translation;
        }

        private static Type GetProviderType(SqlExpression expression)
            => (expression.TypeMapping?.Converter?.ProviderClrType
                ?? expression.TypeMapping?.ClrType
                ?? expression.Type).UnwrapNullableType();
    }
}
