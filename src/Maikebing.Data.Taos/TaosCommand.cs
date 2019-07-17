// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Maikebing.Data.Taos
{
    /// <summary>
    ///     Represents a SQL statement to be executed against a Taos database.
    /// </summary>
    public class TaosCommand : DbCommand
    {
        private readonly Lazy<TaosParameterCollection> _parameters = new Lazy<TaosParameterCollection>(
            () => new TaosParameterCollection());

        private TaosConnection _connection;
        private string _commandText;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TaosCommand" /> class.
        /// </summary>
        public TaosCommand()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TaosCommand" /> class.
        /// </summary>
        /// <param name="commandText">The SQL to execute against the database.</param>
        public TaosCommand(string commandText)
            => CommandText = commandText;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TaosCommand" /> class.
        /// </summary>
        /// <param name="commandText">The SQL to execute against the database.</param>
        /// <param name="connection">The connection used by the command.</param>
        public TaosCommand(string commandText, TaosConnection connection)
            : this(commandText)
        {
            Connection = connection;
            CommandTimeout = connection.DefaultTimeout;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TaosCommand" /> class.
        /// </summary>
        /// <param name="commandText">The SQL to execute against the database.</param>
        /// <param name="connection">The connection used by the command.</param>
        /// <param name="transaction">The transaction within which the command executes.</param>
        public TaosCommand(string commandText, TaosConnection connection, TaosTransaction transaction)
            : this(commandText, connection)
            => Transaction = transaction;

        /// <summary>
        ///     Gets or sets a value indicating how <see cref="CommandText" /> is interpreted. Only
        ///     <see cref="CommandType.Text" /> is supported.
        /// </summary>
        /// <value>A value indicating how <see cref="CommandText" /> is interpreted.</value>
        public override CommandType CommandType
        {
            get => CommandType.Text;
            set
            {
                if (value != CommandType.Text)
                {
                    throw new ArgumentException($"Invalid CommandType{value}");
                }
            }
        }

        /// <summary>
        ///     Gets or sets the SQL to execute against the database.
        /// </summary>
        /// <value>The SQL to execute against the database.</value>
        public override string CommandText
        {
            get => _commandText;
            set
            {
                if (DataReader != null)
                {
                    throw new InvalidOperationException($"SetRequiresNoOpenReader{nameof(CommandText)}");
                }

                if (value != _commandText)
                {
                    _commandText = value;
                }
            }
        }

        /// <summary>
        ///     Gets or sets the connection used by the command.
        /// </summary>
        /// <value>The connection used by the command.</value>
        public new virtual TaosConnection Connection
        {
            get => _connection;
            set
            {
                if (DataReader != null)
                {
                    throw new InvalidOperationException($"SetRequiresNoOpenReader{nameof(Connection)}");
                }

                if (value != _connection)
                {

                    _connection?.RemoveCommand(this);
                    _connection = value;
                    value?.AddCommand(this);
                }
            }
        }

        /// <summary>
        ///     Gets or sets the connection used by the command. Must be a <see cref="TaosConnection" />.
        /// </summary>
        /// <value>The connection used by the command.</value>
        protected override DbConnection DbConnection
        {
            get => Connection;
            set => Connection = (TaosConnection)value;
        }

        /// <summary>
        ///     Gets or sets the transaction within which the command executes.
        /// </summary>
        /// <value>The transaction within which the command executes.</value>
        public new virtual TaosTransaction Transaction { get; set; }

        /// <summary>
        ///     Gets or sets the transaction within which the command executes. Must be a <see cref="TaosTransaction" />.
        /// </summary>
        /// <value>The transaction within which the command executes.</value>
        protected override DbTransaction DbTransaction
        {
            get => Transaction;
            set => Transaction = (TaosTransaction)value;
        }

        /// <summary>
        ///     Gets the collection of parameters used by the command.
        /// </summary>
        /// <value>The collection of parameters used by the command.</value>
        public new virtual TaosParameterCollection Parameters
            => _parameters.Value;

        /// <summary>
        ///     Gets the collection of parameters used by the command.
        /// </summary>
        /// <value>The collection of parameters used by the command.</value>
        protected override DbParameterCollection DbParameterCollection
            => Parameters;

        /// <summary>
        ///     Gets or sets the number of seconds to wait before terminating the attempt to execute the command. Defaults to 30.
        /// </summary>
        /// <value>The number of seconds to wait before terminating the attempt to execute the command.</value>
        /// <remarks>
        ///     The timeout is used when the command is waiting to obtain a lock on the table.
        /// </remarks>
        public override int CommandTimeout { get; set; } = 30;

        /// <summary>
        ///     Gets or sets a value indicating whether the command should be visible in an interface control.
        /// </summary>
        /// <value>A value indicating whether the command should be visible in an interface control.</value>
        public override bool DesignTimeVisible { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating how the results are applied to the row being updated.
        /// </summary>
        /// <value>A value indicating how the results are applied to the row being updated.</value>
        public override UpdateRowSource UpdatedRowSource { get; set; }

        /// <summary>
        ///     Gets or sets the data reader currently being used by the command, or null if none.
        /// </summary>
        /// <value>The data reader currently being used by the command.</value>
        protected internal virtual TaosDataReader DataReader { get; set; }

        /// <summary>
        ///     Releases any resources used by the connection and closes it.
        /// </summary>
        /// <param name="disposing">
        ///     true to release managed and unmanaged resources; false to release only unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {

            base.Dispose(disposing);
        }

        /// <summary>
        ///     Creates a new parameter.
        /// </summary>
        /// <returns>The new parameter.</returns>
        public new virtual TaosParameter CreateParameter()
            => new TaosParameter();

        /// <summary>
        ///     Creates a new parameter.
        /// </summary>
        /// <returns>The new parameter.</returns>
        protected override DbParameter CreateDbParameter()
            => CreateParameter();

        /// <summary>
        ///     Creates a prepared version of the command on the database.
        /// </summary>
        public override void Prepare()
        {
            if (_connection?.State != ConnectionState.Open)
            {
                throw new InvalidOperationException($"CallRequiresOpenConnection{nameof(Prepare)}");
            }

            if (string.IsNullOrEmpty(_commandText))
            {
                throw new InvalidOperationException($"CallRequiresSetCommandText{nameof(Prepare)}");
            }
 
            var timer = Stopwatch.StartNew();

            try
            {
                
            }
            catch
            {

                throw;
            }
        }

        /// <summary>
        ///     Executes the <see cref="CommandText" /> against the database and returns a data reader.
        /// </summary>
        /// <returns>The data reader.</returns>
        /// <exception cref="TaosException">A Taos error occurs during execution.</exception>
        public new virtual TaosDataReader ExecuteReader()
            => ExecuteReader(CommandBehavior.Default);

        /// <summary>
        ///     Executes the <see cref="CommandText" /> against the database and returns a data reader.
        /// </summary>
        /// <param name="behavior">
        ///     A description of the results of the query and its effect on the database.
        ///     <para>
        ///         Only <see cref="CommandBehavior.Default" />, <see cref="CommandBehavior.SequentialAccess" />,
        ///         <see cref="CommandBehavior.SingleResult" />, <see cref="CommandBehavior.SingleRow" />, and
        ///         <see cref="CommandBehavior.CloseConnection" /> are supported.
        ///     </para>
        /// </param>
        /// <returns>The data reader.</returns>
        /// <exception cref="TaosException">A Taos error occurs during execution.</exception>
        public new virtual TaosDataReader ExecuteReader(CommandBehavior behavior)
        {
            if ((behavior & ~(CommandBehavior.Default | CommandBehavior.SequentialAccess | CommandBehavior.SingleResult
                              | CommandBehavior.SingleRow | CommandBehavior.CloseConnection)) != 0)
            {
                throw new ArgumentException($"InvalidCommandBehavior{behavior}");
            }

            if (DataReader != null)
            {
                throw new InvalidOperationException($"DataReaderOpen");
            }

            if (_connection?.State != ConnectionState.Open)
            {
                throw new InvalidOperationException($"CallRequiresOpenConnection{nameof(ExecuteReader)}");
            }

            if (string.IsNullOrEmpty(_commandText))
            {
                throw new InvalidOperationException($"CallRequiresSetCommandText{nameof(ExecuteReader)}");
            }

            if (Transaction != _connection.Transaction)
            {
                throw new InvalidOperationException(
                    Transaction == null
                        ? "TransactionRequired"
                        : "TransactionConnectionMismatch");
            }
            if (_connection.Transaction?.ExternalRollback == true)
            {
                throw new InvalidOperationException("TransactionCompleted");
            }

            var hasChanges = false;
            var changes = 0;
            int rc;
            //var stmts = new Queue<(Taos3_stmt, bool)>();
            var unprepared=false;// _preparedStatements.Count == 0;
            //var timer = Stopwatch.StartNew();

            try
            {
                //    foreach (var stmt in unprepared
                //        ? PrepareAndEnumerateStatements(timer)
                //        : _preparedStatements)
                //    {
                //        var boundParams = 0;

                //        if (_parameters.IsValueCreated)
                //        {
                //            boundParams = _parameters.Value.Bind(stmt);
                //        }

                //        var expectedParams = raw.Taos3_bind_parameter_count(stmt);
                //        if (expectedParams != boundParams)
                //        {
                //            var unboundParams = new List<string>();
                //            for (var i = 1; i <= expectedParams; i++)
                //            {
                //                var name = raw.Taos3_bind_parameter_name(stmt, i);

                //                if (_parameters.IsValueCreated
                //                    || !_parameters.Value.Cast<TaosParameter>().Any(p => p.ParameterName == name))
                //                {
                //                    unboundParams.Add(name);
                //                }
                //            }

                //            throw new InvalidOperationException(Resources.MissingParameters(string.Join(", ", unboundParams)));
                //        }

                //        while (IsBusy(rc = raw.Taos3_step(stmt)))
                //        {
                //            if (timer.ElapsedMilliseconds >= CommandTimeout * 1000)
                //            {
                //                break;
                //            }

                //            raw.Taos3_reset(stmt);

                //            // TODO: Consider having an async path that uses Task.Delay()
                //            Thread.Sleep(150);
                //        }

                //        TaosException.ThrowExceptionForRC(rc, _connection.Handle);

                //        if (rc == raw.Taos_ROW
                //            // NB: This is only a heuristic to separate SELECT statements from INSERT/UPDATE/DELETE statements.
                //            //     It will result in false positives, but it's the best we can do without re-parsing SQL
                //            || raw.Taos3_stmt_readonly(stmt) != 0)
                //        {
                //            stmts.Enqueue((stmt, rc != raw.Taos_DONE));
                //        }
                //        else
                //        {
                //            raw.Taos3_reset(stmt);
                //            hasChanges = true;
                //            changes += raw.Taos3_changes(_connection.Handle);
                //        }
                //    }
            }
            catch when (unprepared)
            {

                throw;
            }

            var closeConnection = (behavior & CommandBehavior.CloseConnection) != 0;
            //TODO: 这里要实现代码
            return null;
        }

        /// <summary>
        ///     Executes the <see cref="CommandText" /> against the database and returns a data reader.
        /// </summary>
        /// <param name="behavior">A description of query's results and its effect on the database.</param>
        /// <returns>The data reader.</returns>
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
            => ExecuteReader(behavior);

        /// <summary>
        ///     Executes the <see cref="CommandText" /> asynchronously against the database and returns a data reader.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        ///     Taos does not support asynchronous execution. Use write-ahead logging instead.
        /// </remarks>
        /// <seealso href="http://Taos.org/wal.html">Write-Ahead Logging</seealso>
        public new virtual Task<TaosDataReader> ExecuteReaderAsync()
            => ExecuteReaderAsync(CommandBehavior.Default, CancellationToken.None);

        /// <summary>
        ///     Executes the <see cref="CommandText" /> asynchronously against the database and returns a data reader.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        ///     Taos does not support asynchronous execution. Use write-ahead logging instead.
        /// </remarks>
        /// <seealso href="http://Taos.org/wal.html">Write-Ahead Logging</seealso>
        public new virtual Task<TaosDataReader> ExecuteReaderAsync(CancellationToken cancellationToken)
            => ExecuteReaderAsync(CommandBehavior.Default, cancellationToken);

        /// <summary>
        ///     Executes the <see cref="CommandText" /> asynchronously against the database and returns a data reader.
        /// </summary>
        /// <param name="behavior">A description of query's results and its effect on the database.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        ///     Taos does not support asynchronous execution. Use write-ahead logging instead.
        /// </remarks>
        /// <seealso href="http://Taos.org/wal.html">Write-Ahead Logging</seealso>
        public new virtual Task<TaosDataReader> ExecuteReaderAsync(CommandBehavior behavior)
            => ExecuteReaderAsync(behavior, CancellationToken.None);

        /// <summary>
        ///     Executes the <see cref="CommandText" /> asynchronously against the database and returns a data reader.
        /// </summary>
        /// <param name="behavior">A description of query's results and its effect on the database.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        ///     Taos does not support asynchronous execution. Use write-ahead logging instead.
        /// </remarks>
        /// <seealso href="http://Taos.org/wal.html">Write-Ahead Logging</seealso>
        public new virtual Task<TaosDataReader> ExecuteReaderAsync(
            CommandBehavior behavior,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(ExecuteReader(behavior));
        }

        /// <summary>
        ///     Executes the <see cref="CommandText" /> asynchronously against the database and returns a data reader.
        /// </summary>
        /// <param name="behavior">A description of query's results and its effect on the database.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(
            CommandBehavior behavior,
            CancellationToken cancellationToken)
            => await ExecuteReaderAsync(behavior, cancellationToken);

        /// <summary>
        ///     Executes the <see cref="CommandText" /> against the database.
        /// </summary>
        /// <returns>The number of rows inserted, updated, or deleted. -1 for SELECT statements.</returns>
        /// <exception cref="TaosException">A Taos error occurs during execution.</exception>
        public override int ExecuteNonQuery()
        {
            if (_connection?.State != ConnectionState.Open)
            {
                throw new InvalidOperationException($"CallRequiresOpenConnection{nameof(ExecuteNonQuery)}");
            }
            if (_commandText == null)
            {
                throw new InvalidOperationException($"CallRequiresSetCommandText{nameof(ExecuteNonQuery)}");
            }

            var reader = ExecuteReader();
            reader.Dispose();

            return reader.RecordsAffected;
        }

        /// <summary>
        ///     Executes the <see cref="CommandText" /> against the database and returns the result.
        /// </summary>
        /// <returns>The first column of the first row of the results, or null if no results.</returns>
        /// <exception cref="TaosException">A Taos error occurs during execution.</exception>
        public override object ExecuteScalar()
        {
            if (_connection?.State != ConnectionState.Open)
            {
                throw new InvalidOperationException($"CallRequiresOpenConnection{nameof(ExecuteScalar)}");
            }
            if (_commandText == null)
            {
                throw new InvalidOperationException($"CallRequiresSetCommandText{nameof(ExecuteScalar)}");
            }

            using (var reader = ExecuteReader())
            {
                return reader.Read()
                    ? reader.GetValue(0)
                    : null;
            }
        }

        /// <summary>
        ///     Attempts to cancel the execution of the command. Does nothing.
        /// </summary>
        public override void Cancel()
        {
        }

      
    }
}
