// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Maikebing.Data.Taos;
using Microsoft.EntityFrameworkCore.Taos.Internal;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.Taos.Update.Internal
{
    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class TaosUpdateSqlGenerator : UpdateSqlGenerator
    {
        private readonly TaosConnectionStringBuilder _taosConnectionStringBuilder;
        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public TaosUpdateSqlGenerator([NotNull] UpdateSqlGeneratorDependencies dependencies, TaosConnectionStringBuilder taosConnectionStringBuilder)
            : base(dependencies)
        {
            _taosConnectionStringBuilder = taosConnectionStringBuilder;
        }
        protected override void AppendSelectCommandHeader([NotNull] StringBuilder commandStringBuilder, [NotNull] IReadOnlyList<ColumnModification> operations)
        {
            base.AppendSelectCommandHeader(commandStringBuilder, operations);
        }
        public override ResultSetMapping AppendInsertOperation(StringBuilder commandStringBuilder, ModificationCommand command, int commandPosition)
        {
            Check.NotNull(commandStringBuilder, nameof(commandStringBuilder));
            Check.NotNull(command, nameof(command));

            var name = command.TableName;
            var schema = command.Schema;
            if (string.IsNullOrEmpty(schema)) schema = _taosConnectionStringBuilder.DataBase;
            var operations = command.ColumnModifications;

            var writeOperations = operations.Where(o => o.IsWrite).ToList();
            var readOperations = operations.Where(o => o.IsRead).ToList();

            AppendInsertCommand(commandStringBuilder, name, schema, writeOperations);

            if (readOperations.Count > 0)
            {
                var keyOperations = operations.Where(o => o.IsKey).ToList();

                return AppendSelectAffectedCommand(commandStringBuilder, name, schema, readOperations, keyOperations, commandPosition);
            }

            return ResultSetMapping.NoResultSet;
        }

        /// <summary>
        ///     Appends a SQL command for selecting affected data.
        /// </summary>
        /// <param name="commandStringBuilder"> The builder to which the SQL should be appended. </param>
        /// <param name="name"> The name of the table. </param>
        /// <param name="schema"> The table schema, or <c>null</c> to use the default schema. </param>
        /// <param name="readOperations"> The operations representing the data to be read. </param>
        /// <param name="conditionOperations"> The operations used to generate the <c>WHERE</c> clause for the select. </param>
        /// <param name="commandPosition"> The ordinal of the command for which rows affected it being returned. </param>
        /// <returns> The <see cref="ResultSetMapping" /> for this command.</returns>
        protected override  ResultSetMapping AppendSelectAffectedCommand(
            [NotNull] StringBuilder commandStringBuilder,
            [NotNull] string name,
            [CanBeNull] string schema,
            [NotNull] IReadOnlyList<ColumnModification> readOperations,
            [NotNull] IReadOnlyList<ColumnModification> conditionOperations,
            int commandPosition)
        {
            Check.NotNull(commandStringBuilder, nameof(commandStringBuilder));
            Check.NotEmpty(name, nameof(name));
            Check.NotNull(readOperations, nameof(readOperations));
            Check.NotNull(conditionOperations, nameof(conditionOperations));

            AppendSelectCommandHeader(commandStringBuilder, readOperations);
            AppendFromClause(commandStringBuilder, name, schema);
            // TODO: there is no notion of operator - currently all the where conditions check equality
            AppendWhereAffectedClause(commandStringBuilder, conditionOperations);
            commandStringBuilder.AppendLine(SqlGenerationHelper.StatementTerminator)
                .AppendLine();

            return ResultSetMapping.LastInResultSet;
        }

        protected override void AppendFromClause(
          [NotNull] StringBuilder commandStringBuilder,
          [NotNull] string name,
          [CanBeNull] string schema)
        {
            Check.NotNull(commandStringBuilder, nameof(commandStringBuilder));
            Check.NotEmpty(name, nameof(name));

            commandStringBuilder
                .AppendLine()
                .Append("FROM ");
            SqlGenerationHelper.DelimitIdentifier(commandStringBuilder, name, schema);
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected override void AppendIdentityWhereCondition(StringBuilder commandStringBuilder, ColumnModification columnModification)
        {
            Check.NotNull(commandStringBuilder, nameof(commandStringBuilder));
            Check.NotNull(columnModification, nameof(columnModification));

            SqlGenerationHelper.DelimitIdentifier(commandStringBuilder, columnModification.ColumnName);
            commandStringBuilder.Append(" = ")
                .Append("last_insert_rowid()");
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected override ResultSetMapping AppendSelectAffectedCountCommand(StringBuilder commandStringBuilder, string name, string schema, int commandPosition)
        {
            Check.NotNull(commandStringBuilder, nameof(commandStringBuilder));
            Check.NotEmpty(name, nameof(name));

            commandStringBuilder
                .Append("SELECT changes()")
                .AppendLine(SqlGenerationHelper.StatementTerminator)
                .AppendLine();

            return ResultSetMapping.LastInResultSet;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected override void AppendRowsAffectedWhereCondition(StringBuilder commandStringBuilder, int expectedRowsAffected)
        {
            Check.NotNull(commandStringBuilder, nameof(commandStringBuilder));

            commandStringBuilder.Append("changes() = ").Append(expectedRowsAffected);
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override string GenerateNextSequenceValueOperation(string name, string schema)
        {
            throw new NotSupportedException(TaosStrings.SequencesNotSupported);
        }
    }
}
