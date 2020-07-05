// Copyright (c)  maikebing All rights reserved.
//// Licensed under the MIT License, See License.txt in the project root for license information.

using JetBrains.Annotations;
using Maikebing.Data.Taos;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Utilities;
using Remotion.Linq.Clauses;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.Taos.Query.Sql.Internal
{
    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class TaosQuerySqlGenerator : DefaultQuerySqlGenerator
    {
        private readonly TaosConnectionStringBuilder __taosConnectionStringBuilder;

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public TaosQuerySqlGenerator(
            [NotNull] QuerySqlGeneratorDependencies dependencies,
            [NotNull] SelectExpression selectExpression,
             TaosConnectionStringBuilder _taosConnectionStringBuilder)
            : base(dependencies, selectExpression)
        {
            __taosConnectionStringBuilder = _taosConnectionStringBuilder;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected override string TypedTrueLiteral => "1";

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected override string TypedFalseLiteral => "0";

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected override string GenerateOperator(Expression expression)
            => expression.NodeType == ExpressionType.Add && expression.Type == typeof(string)
                ? " || "
                : base.GenerateOperator(expression);

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected override void GenerateTop(SelectExpression selectExpression)
        {
            // Handled by GenerateLimitOffset
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected override void GenerateLimitOffset(SelectExpression selectExpression)
        {
            Check.NotNull(selectExpression, nameof(selectExpression));

            if (selectExpression.Limit != null
                || selectExpression.Offset != null)
            {
                Sql.AppendLine()
                    .Append("LIMIT ");

                Visit(selectExpression.Limit ?? Expression.Constant(-1));

                if (selectExpression.Offset != null)
                {
                    Sql.Append(" OFFSET ");

                    Visit(selectExpression.Offset);
                }
            }
        }

        private IRelationalCommandBuilder _rcbsql => Sql;

        public override Expression VisitSelect(SelectExpression selectExpression)
        {
            Check.NotNull(selectExpression, nameof(selectExpression));

            IDisposable subQueryIndent = null;

            if (selectExpression.Alias != null)
            {
                _rcbsql.AppendLine("(");

                subQueryIndent = _rcbsql.Indent();
            }

            _rcbsql.Append("SELECT ");

            var projectionAdded = false;

            if (selectExpression.IsProjectStar)
            {
                _rcbsql.Append("*");
                projectionAdded = true;
            }

            if (selectExpression.Projection.Count > 0)
            {
                if (selectExpression.IsProjectStar)
                {
                    _rcbsql.Append(", ");
                }

                for (int i = 0; i < selectExpression.Projection.Count; i++)
                {
                    var col = (ColumnExpression)selectExpression.Projection[i];
                    _rcbsql.Append($"{col.Name} ");
                    if (i < selectExpression.Projection.Count - 1)
                    {
                        _rcbsql.Append(", ");
                    }
                }
                projectionAdded = true;
            }

            if (!projectionAdded)
            {
                _rcbsql.Append("1");
            }

            if (selectExpression.Tables.Count > 0)
            {
                _rcbsql.AppendLine()
                    .Append("FROM ");

                GenerateList(selectExpression.Tables, tx =>
                 {
                     var te = ((TableExpression)tx);
                     var schema = string.IsNullOrEmpty(te.Schema) ? __taosConnectionStringBuilder.DataBase : te.Schema;
                     _rcbsql.Append($"{schema}.{te.Table}");
                 }, null);
            }
            else
            {
                GeneratePseudoFromClause();
            }

            if (selectExpression.Predicate != null)
            {
                GeneratePredicate(selectExpression.Predicate);
            }

            if (selectExpression.GroupBy.Count > 0)
            {
                _rcbsql.AppendLine();

                _rcbsql.Append("GROUP BY ");
                GenerateList(selectExpression.GroupBy);
            }

            if (selectExpression.Having != null)
            {
                GenerateHaving(selectExpression.Having);
            }

            if (selectExpression.OrderBy.Count > 0)
            {
                var orderByList = new List<Ordering>(selectExpression.OrderBy);

                // Filter out constant and parameter expressions (SELECT 1) if there is no skip or take #10410
                if (selectExpression.Limit == null && selectExpression.Offset == null)
                {
                    //   orderByList.RemoveAll(o => IsOrderByExpressionConstant(ApplyOptimizations(o.Expression, searchCondition: false)));
                }

                if (orderByList.Count > 0)
                {
                    _rcbsql.AppendLine();

                    GenerateOrderBy(orderByList);
                }
            }

            GenerateLimitOffset(selectExpression);

            if (subQueryIndent != null)
            {
                subQueryIndent.Dispose();

                _rcbsql.AppendLine()
                    .Append(")");

                if (selectExpression.Alias.Length > 0)
                {
                    _rcbsql
                        .Append(AliasSeparator)
                        .Append(SqlGenerator.DelimitIdentifier(selectExpression.Alias));
                }
            }

            return selectExpression;
        }

        public override Expression VisitColumn(ColumnExpression columnExpression)
        {
            _rcbsql.Append(columnExpression.Name + " ");
            return columnExpression;
        }
    }
}