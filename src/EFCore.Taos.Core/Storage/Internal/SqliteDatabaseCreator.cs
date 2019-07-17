// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Maikebing.Data.Taos;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Taos.Storage.Internal
{
    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class TaosDatabaseCreator : RelationalDatabaseCreator
    {
        // ReSharper disable once InconsistentNaming
        private const int Taos_CANTOPEN = 14;

        private readonly ITaosRelationalConnection _connection;
        private readonly IRawSqlCommandBuilder _rawSqlCommandBuilder;

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public TaosDatabaseCreator(
            [NotNull] RelationalDatabaseCreatorDependencies dependencies,
            [NotNull] ITaosRelationalConnection connection,
            [NotNull] IRawSqlCommandBuilder rawSqlCommandBuilder)
            : base(dependencies)
        {
            _connection = connection;
            _rawSqlCommandBuilder = rawSqlCommandBuilder;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override void Create()
        {
            Dependencies.Connection.Open();

            _rawSqlCommandBuilder
                .Build($"create database {Dependencies.Connection.DbConnection.Database};")
                .ExecuteNonQuery(Dependencies.Connection);

            Dependencies.Connection.Close();
        }
        List<_SHOWDATABASES> _SHOWDATABASEs;
        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override bool Exists()
        {
            using (var readOnlyConnection = _connection.CreateReadOnlyConnection())
            {
                try
                {

                    readOnlyConnection.Open(errorsExpected: true);
                    _SHOWDATABASEs = _rawSqlCommandBuilder
                                 .Build($"SHOW DATABASES;")
                                 .ExecuteReader(Dependencies.Connection)
                                 .DbDataReader
                                 .ToObject<_SHOWDATABASES>();
                    return _SHOWDATABASEs!=null &&  _SHOWDATABASEs.Any(m => m.name == _connection.DbConnection.Database);
                }
                catch (TaosException ex) when (ex.TaosErrorCode == Taos_CANTOPEN)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected override bool HasTables()
        {
            var count = _SHOWDATABASEs?
                .Find(db=> db.name==_connection.DbConnection.Database)
                ?.ntables;
            return count.HasValue && count != 0;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override void Delete()
        {
            Dependencies.Connection.Open();
            try
            {
                _rawSqlCommandBuilder
                          .Build($"DROP DATABASE    {Dependencies.Connection.DbConnection.Database};")
                          .ExecuteNonQuery(Dependencies.Connection);

            }
            catch
            {
                // any exceptions here can be ignored
            }
            finally
            {
                Dependencies.Connection.Close();
            }
        }
    }
}
