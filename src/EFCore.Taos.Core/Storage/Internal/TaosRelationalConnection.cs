// Copyright (c)  maikebing All rights reserved.
//// Licensed under the MIT License, See License.txt in the project root for license information.

using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Maikebing.Data.Taos;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Taos.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.Taos.Storage.Internal
{
    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class TaosRelationalConnection : RelationalConnection, ITaosRelationalConnection
    {
        private readonly IRawSqlCommandBuilder _rawSqlCommandBuilder;
        private readonly bool _loadSpatialite;
        private readonly bool _enforceForeignKeys = true;

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public TaosRelationalConnection(
            [NotNull] RelationalConnectionDependencies dependencies,
            [NotNull] IRawSqlCommandBuilder rawSqlCommandBuilder)
            : base(dependencies)
        {
            Check.NotNull(rawSqlCommandBuilder, nameof(rawSqlCommandBuilder));

            _rawSqlCommandBuilder = rawSqlCommandBuilder;

            var optionsExtension = dependencies.ContextOptions.Extensions.OfType<TaosOptionsExtension>().FirstOrDefault();
            if (optionsExtension != null)
            {
                _loadSpatialite = optionsExtension.LoadSpatialite;
                _enforceForeignKeys = optionsExtension.EnforceForeignKeys;
            }
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected override DbConnection CreateDbConnection() => new TaosConnection(ConnectionString);

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override bool IsMultipleActiveResultSetsEnabled => true;

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override bool Open(bool errorsExpected = false)
        {
            if (base.Open(errorsExpected))
            {
              //  LoadSpatialite();
                //EnableForeignKeys();
                return true;
            }

            return false;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override async Task<bool> OpenAsync(CancellationToken cancellationToken, bool errorsExpected = false)
        {
            if (await base.OpenAsync(cancellationToken, errorsExpected))
            {
              //  LoadSpatialite();
                //EnableForeignKeys();
                return true;
            }

            return false;
        }

        private void LoadSpatialite()
        {
            if (!_loadSpatialite)
            {
                return;
            }

            var connection = (TaosConnection)DbConnection;
            SpatialiteLoader.Load(DbConnection);
        }

        private void EnableForeignKeys()
        {
            if (_enforceForeignKeys)
            {
                _rawSqlCommandBuilder.Build("PRAGMA foreign_keys=ON;").ExecuteNonQuery(this);
            }
            else
            {
                _rawSqlCommandBuilder.Build("PRAGMA foreign_keys=OFF;").ExecuteNonQuery(this);
            }
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual ITaosRelationalConnection CreateReadOnlyConnection()
        {
            var connectionStringBuilder = new TaosConnectionStringBuilder(ConnectionString)
            {
            };

            var contextOptions = new DbContextOptionsBuilder().UseTaos(connectionStringBuilder.ToString()).Options;

            return new TaosRelationalConnection(Dependencies.With(contextOptions), _rawSqlCommandBuilder);
        }
    }
}
