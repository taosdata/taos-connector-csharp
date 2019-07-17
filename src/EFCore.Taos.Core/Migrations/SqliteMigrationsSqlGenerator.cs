// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Taos.Internal;
using Microsoft.EntityFrameworkCore.Taos.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Taos.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Utilities;
using Maikebing.Data.Taos;

namespace Microsoft.EntityFrameworkCore.Migrations
{
    /// <summary>
    ///     Taos-specific implementation of <see cref="MigrationsSqlGenerator" />.
    /// </summary>
    public class TaosMigrationsSqlGenerator : MigrationsSqlGenerator
    {
        private readonly IMigrationsAnnotationProvider _migrationsAnnotations;
        private readonly TaosConnectionStringBuilder _taosConnectionStringBuilder;
        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        /// <param name="dependencies"> Parameter object containing dependencies for this service. </param>
        /// <param name="migrationsAnnotations"> Provider-specific Migrations annotations to use. </param>
        public TaosMigrationsSqlGenerator(
            [NotNull] MigrationsSqlGeneratorDependencies dependencies,
            TaosConnectionStringBuilder taosConnectionStringBuilder,
            [NotNull] IMigrationsAnnotationProvider migrationsAnnotations)
            : base(dependencies)
        {
            _taosConnectionStringBuilder = taosConnectionStringBuilder;
            _migrationsAnnotations = migrationsAnnotations;
        }

        /// <summary>
        ///     Generates commands from a list of operations.
        /// </summary>
        /// <param name="operations"> The operations. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <returns> The list of commands to be executed or scripted. </returns>
        public override IReadOnlyList<MigrationCommand> Generate(IReadOnlyList<MigrationOperation> operations, IModel model = null)
            => base.Generate(RewriteOperations(operations, model), model);

        private bool IsSpatialiteColumn(AddColumnOperation operation, IModel model)
            => TaosTypeMappingSource.IsSpatialiteType(
                operation.ColumnType
                    ?? GetColumnType(
                        operation.Schema,
                        operation.Table,
                        operation.Name,
                        operation.ClrType,
                        operation.IsUnicode,
                        operation.MaxLength,
                        operation.IsFixedLength,
                        operation.IsRowVersion,
                        model));

        private IReadOnlyList<MigrationOperation> RewriteOperations(
            IReadOnlyList<MigrationOperation> migrationOperations,
            IModel model)
        {
            var operations = new List<MigrationOperation>();

            
            foreach (var operation in migrationOperations)
            {
                if (operation is AddForeignKeyOperation foreignKeyOperation)
                {
                    var table = migrationOperations
                        .OfType<CreateTableOperation>()
                        .FirstOrDefault(o => o.Name == foreignKeyOperation.Table);
                    table.Schema = _taosConnectionStringBuilder.DataBase;
                    if (table != null)
                    {
                        table.ForeignKeys.Add(foreignKeyOperation);
                    }
                    else
                    {
                        operations.Add(operation);
                    }
                }
                else if (operation is CreateTableOperation createTableOperation)
                {
                    var spatialiteColumns = new Stack<AddColumnOperation>();
                    createTableOperation.Schema = _taosConnectionStringBuilder.DataBase;
                    for (var i = createTableOperation.Columns.Count - 1; i >= 0; i--)
                    {
                        var addColumnOperation = createTableOperation.Columns[i];

                        if (IsSpatialiteColumn(addColumnOperation, model))
                        {
                            spatialiteColumns.Push(addColumnOperation);
                            createTableOperation.Columns.RemoveAt(i);
                        }
                    }

                    operations.Add(operation);
                    operations.AddRange(spatialiteColumns);
                }
                else
                {
                    operations.Add(operation);
                }
            }

            return operations;
        }

        /// <summary>
        ///     Builds commands for the given <see cref="AlterDatabaseOperation" /> by making calls on the given
        ///     <see cref="MigrationCommandListBuilder" />.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(AlterDatabaseOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            if (operation[TaosAnnotationNames.InitSpatialMetaData] as bool? != true
                || operation.OldDatabase[TaosAnnotationNames.InitSpatialMetaData] as bool? == true)
            {
                return;
            }

            builder
                .Append("SELECT InitSpatialMetaData()")
                .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
            EndStatement(builder);
        }

        /// <summary>
        ///     Builds commands for the given <see cref="AddColumnOperation" /> by making calls on the given
        ///     <see cref="MigrationCommandListBuilder" />.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        /// <param name="terminate"> Indicates whether or not to terminate the command after generating SQL for the operation. </param>
        protected override void Generate(AddColumnOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate)
        {
            if (!IsSpatialiteColumn(operation, model))
            {
                base.Generate(operation, model, builder, terminate);

                return;
            }

            var stringTypeMapping = Dependencies.TypeMappingSource.GetMapping(typeof(string));
            var longTypeMapping = Dependencies.TypeMappingSource.GetMapping(typeof(long));

            var srid = operation[TaosAnnotationNames.Srid] as int? ?? 0;
            var dimension = operation[TaosAnnotationNames.Dimension] as string;

            var geometryType = operation.ColumnType
                ?? GetColumnType(
                    operation.Schema,
                    operation.Table,
                    operation.Name,
                    operation.ClrType,
                    operation.IsUnicode,
                    operation.MaxLength,
                    operation.IsFixedLength,
                    operation.IsRowVersion,
                    model);
            if (!string.IsNullOrEmpty(dimension))
            {
                geometryType += dimension;
            }

            builder
                .Append("SELECT AddGeometryColumn(")
                .Append(stringTypeMapping.GenerateSqlLiteral(operation.Table))
                .Append(", ")
                .Append(stringTypeMapping.GenerateSqlLiteral(operation.Name))
                .Append(", ")
                .Append(longTypeMapping.GenerateSqlLiteral(srid))
                .Append(", ")
                .Append(stringTypeMapping.GenerateSqlLiteral(geometryType))
                .Append(", -1, ")
                .Append(operation.IsNullable ? "0" : "1")
                .Append(")");

            if (terminate)
            {
                builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
                EndStatement(builder);
            }
            else
            {
                Debug.Fail("I have a bad feeling about this. Geometry columns don't compose well.");
            }
        }

        /// <summary>
        ///     Builds commands for the given <see cref="DropIndexOperation" />
        ///     by making calls on the given <see cref="MigrationCommandListBuilder" />.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        /// <param name="terminate"> Indicates whether or not to terminate the command after generating SQL for the operation. </param>
        protected override void Generate([NotNull] DropIndexOperation operation, [CanBeNull] IModel model, [NotNull] MigrationCommandListBuilder builder)
        {
     
     
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            builder
                .Append("DROP INDEX ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name));

                builder
                    .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator)
                    .EndCommand();
        }

        /// <summary>
        ///     Builds commands for the given <see cref="RenameIndexOperation" />
        ///     by making calls on the given <see cref="MigrationCommandListBuilder" />.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(RenameIndexOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            var index = FindEntityTypes(model, operation.Schema, operation.Table)
                ?.SelectMany(t => t.GetDeclaredIndexes()).Where(i => i.Relational().Name == operation.NewName)
                .FirstOrDefault();
            if (index == null)
            {
                throw new NotSupportedException(
                    TaosStrings.InvalidMigrationOperation(operation.GetType().ShortDisplayName()));
            }

            var dropOperation = new DropIndexOperation
            {
                Schema = operation.Schema,
                Table = operation.Table,
                Name = operation.Name
            };
            dropOperation.AddAnnotations(_migrationsAnnotations.ForRemove(index));

            var createOperation = new CreateIndexOperation
            {
                IsUnique = index.IsUnique,
                Name = operation.NewName,
                Schema = operation.Schema,
                Table = operation.Table,
                Columns = index.Properties.Select(p => p.Relational().ColumnName).ToArray(),
                Filter = index.Relational().Filter
            };
            createOperation.AddAnnotations(_migrationsAnnotations.For(index));

         //   Generate(dropOperation, model, builder, terminate: false);
            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);

            Generate(createOperation, model, builder);
        }

        /// <summary>
        ///     Builds commands for the given <see cref="RenameTableOperation" />
        ///     by making calls on the given <see cref="MigrationCommandListBuilder" />.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(RenameTableOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            if (operation.NewName != null
                && operation.NewName != operation.Name)
            {
                builder
                    .Append("ALTER TABLE ")
                    .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name))
                    .Append(" RENAME TO ")
                    .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.NewName))
                    .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator)
                    .EndCommand();
            }
        }

        /// <summary>
        ///     Builds commands for the given <see cref="RenameTableOperation" />
        ///     by making calls on the given <see cref="MigrationCommandListBuilder" />.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(RenameColumnOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            builder
                .Append("ALTER TABLE ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Table))
                .Append(" RENAME COLUMN ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name))
                .Append(" TO ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.NewName))
                .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator)
                .EndCommand();
        }

        /// <summary>
        ///     Builds commands for the given <see cref="CreateTableOperation" />
        ///     by making calls on the given <see cref="MigrationCommandListBuilder" />.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(CreateTableOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));
          
            // Lifts a primary key definition into the typename.
            // This handles the quirks of creating integer primary keys using autoincrement, not default rowid behavior.
            if (operation.PrimaryKey?.Columns.Length == 1)
            {
                var columnOp = operation.Columns.FirstOrDefault(o => o.Name == operation.PrimaryKey.Columns[0]);
                if (columnOp != null)
                {
                    columnOp.AddAnnotation(TaosAnnotationNames.InlinePrimaryKey, true);
                    if (!string.IsNullOrEmpty(operation.PrimaryKey.Name))
                    {
                        columnOp.AddAnnotation(TaosAnnotationNames.InlinePrimaryKeyName, operation.PrimaryKey.Name);
                    }

                    operation.PrimaryKey = null;
                }
            }
            Generate(operation, model, builder, true);
        }
        

        /// <summary>
        ///     Builds commands for the given <see cref="CreateTableOperation" /> by making calls on the given
        ///     <see cref="MigrationCommandListBuilder" />.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        /// <param name="terminate"> Indicates whether or not to terminate the command after generating SQL for the operation. </param>
        protected  override void Generate(
            [NotNull] CreateTableOperation operation,
            [CanBeNull] IModel model,
            [NotNull] MigrationCommandListBuilder builder,
            bool terminate)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            builder
                .Append("CREATE TABLE ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, operation.Schema))
                .AppendLine(" (");

            using (builder.Indent())
            {
                for (var i = 0; i < operation.Columns.Count; i++)
                {
                    var column = operation.Columns[i];
                    ColumnDefinition(column, model, builder);

                    if (i != operation.Columns.Count - 1)
                    {
                        builder.AppendLine(",");
                    }
                }

                if (operation.PrimaryKey != null)
                {
                    builder.AppendLine(",");
                    PrimaryKeyConstraint(operation.PrimaryKey, model, builder);
                }

                foreach (var uniqueConstraint in operation.UniqueConstraints)
                {
                    builder.AppendLine(",");
                    UniqueConstraint(uniqueConstraint, model, builder);
                }

                foreach (var foreignKey in operation.ForeignKeys)
                {
                    builder.AppendLine(",");
                    ForeignKeyConstraint(foreignKey, model, builder);
                }

                builder.AppendLine();
            }

            builder.Append(")");

            if (terminate)
            {
                builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
                EndStatement(builder);
            }
        }
        /// <summary>
        ///     Generates a SQL fragment for a column definition in an <see cref="AddColumnOperation" />.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to add the SQL fragment. </param>
        protected override void ColumnDefinition(AddColumnOperation operation, IModel model, MigrationCommandListBuilder builder)
            => ColumnDefinition(
                operation.Schema,
                operation.Table,
                operation.Name,
                operation.ClrType,
                operation.ColumnType,
                operation.IsUnicode,
                operation.MaxLength,
                operation.IsFixedLength,
                operation.IsRowVersion,
                operation.IsNullable,
                operation.DefaultValue,
                operation.DefaultValueSql,
                operation.ComputedColumnSql,
                operation,
                model,
                builder);
        protected override  void ColumnDefinition(
         [CanBeNull] string schema,
         [NotNull] string table,
         [NotNull] string name,
         [NotNull] Type clrType,
         [CanBeNull] string type,
         bool? unicode,
         int? maxLength,
         bool? fixedLength,
         bool rowVersion,
         bool nullable,
         [CanBeNull] object defaultValue,
         [CanBeNull] string defaultValueSql,
         [CanBeNull] string computedColumnSql,
         [NotNull] IAnnotatable annotatable,
         [CanBeNull] IModel model,
         [NotNull] MigrationCommandListBuilder builder)
        {
            Check.NotEmpty(name, nameof(name));
            Check.NotNull(clrType, nameof(clrType));
            Check.NotNull(annotatable, nameof(annotatable));
            Check.NotNull(builder, nameof(builder));

            builder
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(name))
                .Append(" ")
                .Append(type ?? GetColumnType(schema, table, name, clrType, unicode, maxLength, fixedLength, rowVersion, model));


            DefaultValue(defaultValue, defaultValueSql, builder);
        }
       

        #region Invalid migration operations

        /// <summary>
        ///     Throws <see cref="NotSupportedException" /> since this operation requires table rebuilds, which
        ///     are not yet supported.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(AddForeignKeyOperation operation, IModel model, MigrationCommandListBuilder builder)
            => throw new NotSupportedException(
                TaosStrings.InvalidMigrationOperation(operation.GetType().ShortDisplayName()));

        /// <summary>
        ///     Throws <see cref="NotSupportedException" /> since this operation requires table rebuilds, which
        ///     are not yet supported.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(AddPrimaryKeyOperation operation, IModel model, MigrationCommandListBuilder builder)
            => throw new NotSupportedException(
                TaosStrings.InvalidMigrationOperation(operation.GetType().ShortDisplayName()));

        /// <summary>
        ///     Throws <see cref="NotSupportedException" /> since this operation requires table rebuilds, which
        ///     are not yet supported.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(AddUniqueConstraintOperation operation, IModel model, MigrationCommandListBuilder builder)
            => throw new NotSupportedException(
                TaosStrings.InvalidMigrationOperation(operation.GetType().ShortDisplayName()));

        /// <summary>
        ///     Throws <see cref="NotSupportedException" /> since this operation requires table rebuilds, which
        ///     are not yet supported.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(DropColumnOperation operation, IModel model, MigrationCommandListBuilder builder)
            => throw new NotSupportedException(
                TaosStrings.InvalidMigrationOperation(operation.GetType().ShortDisplayName()));

        /// <summary>
        ///     Throws <see cref="NotSupportedException" /> since this operation requires table rebuilds, which
        ///     are not yet supported.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(DropForeignKeyOperation operation, IModel model, MigrationCommandListBuilder builder)
            => throw new NotSupportedException(
                TaosStrings.InvalidMigrationOperation(operation.GetType().ShortDisplayName()));

        /// <summary>
        ///     Throws <see cref="NotSupportedException" /> since this operation requires table rebuilds, which
        ///     are not yet supported.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(DropPrimaryKeyOperation operation, IModel model, MigrationCommandListBuilder builder)
            => throw new NotSupportedException(
                TaosStrings.InvalidMigrationOperation(operation.GetType().ShortDisplayName()));

        /// <summary>
        ///     Throws <see cref="NotSupportedException" /> since this operation requires table rebuilds, which
        ///     are not yet supported.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(DropUniqueConstraintOperation operation, IModel model, MigrationCommandListBuilder builder)
            => throw new NotSupportedException(
                TaosStrings.InvalidMigrationOperation(operation.GetType().ShortDisplayName()));

        /// <summary>
        ///     Throws <see cref="NotSupportedException" /> since this operation requires table rebuilds, which
        ///     are not yet supported.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(AlterColumnOperation operation, IModel model, MigrationCommandListBuilder builder)
            => throw new NotSupportedException(
                TaosStrings.InvalidMigrationOperation(operation.GetType().ShortDisplayName()));

        #endregion

        #region Ignored schema operations

        /// <summary>
        ///     Ignored, since schemas are not supported by Taos and are silently ignored to improve testing compatibility.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(EnsureSchemaOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
        }

        /// <summary>
        ///     Ignored, since schemas are not supported by Taos and are silently ignored to improve testing compatibility.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(DropSchemaOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
        }

        #endregion

        #region Sequences not supported

        /// <summary>
        ///     Throws <see cref="NotSupportedException" /> since Taos does not support sequences.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(RestartSequenceOperation operation, IModel model, MigrationCommandListBuilder builder)
            => throw new NotSupportedException(TaosStrings.SequencesNotSupported);

        /// <summary>
        ///     Throws <see cref="NotSupportedException" /> since Taos does not support sequences.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(CreateSequenceOperation operation, IModel model, MigrationCommandListBuilder builder)
            => throw new NotSupportedException(TaosStrings.SequencesNotSupported);

        /// <summary>
        ///     Throws <see cref="NotSupportedException" /> since Taos does not support sequences.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(RenameSequenceOperation operation, IModel model, MigrationCommandListBuilder builder)
            => throw new NotSupportedException(TaosStrings.SequencesNotSupported);

        /// <summary>
        ///     Throws <see cref="NotSupportedException" /> since Taos does not support sequences.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(AlterSequenceOperation operation, IModel model, MigrationCommandListBuilder builder)
            => throw new NotSupportedException(TaosStrings.SequencesNotSupported);

        /// <summary>
        ///     Throws <see cref="NotSupportedException" /> since Taos does not support sequences.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(DropSequenceOperation operation, IModel model, MigrationCommandListBuilder builder)
            => throw new NotSupportedException(TaosStrings.SequencesNotSupported);

        #endregion
    }
}
