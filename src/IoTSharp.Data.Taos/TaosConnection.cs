// Copyright (c)  maikebing All rights reserved.
//// Licensed under the MIT License, See License.txt in the project root for license information.

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TDengineDriver;
using System.Linq;
using IoTSharp.Data.Taos.Driver;
using System.Collections;

namespace IoTSharp.Data.Taos
{
    /// <summary>
    ///     Represents a connection to a Taos database.
    /// </summary>
    public partial class TaosConnection : DbConnection
    {
 
        private readonly IList<WeakReference<TaosCommand>> _commands = new List<WeakReference<TaosCommand>>();
        private static readonly Dictionary<string, ConcurrentTaosQueue> g_pool = new Dictionary<string, ConcurrentTaosQueue>();
        private ConcurrentTaosQueue _queue=null;
        private string _connectionString;
        private ConnectionState _state;

        private static bool  _dll_isloaded=false;
        public TaosConnection():this( string.Empty)
        {

        }
        public TaosConnection(string connectionString) : this(connectionString,string.Empty)
        {

        }
        public TaosConnection(string connectionString, string configdir):this(connectionString,configdir,60,string.Empty,string.Empty,string.Empty)
        {

        }

        /// <summary>
        /// 初始化链接
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="configdir">配置目录 顺序 configdir > BaseDirectory > CurrentDirectory > "/etc/taos" || "C:/TDengine/cfg" </param>
        /// <param name="shell_activity_timer">Shell 活动定时器</param>
        /// <param name="locale">区域 'en_US.UTF-8'</param>
        /// <param name="charset">字符集 'UTF-8'</param>
        /// <param name="timezone">时区 例如  'Asia/Shanghai' </param>
        public TaosConnection(string connectionString,string configdir,int  shell_activity_timer,string locale,string charset, string timezone) 
        {
           
            if (!string.IsNullOrEmpty(connectionString) &&  !string.IsNullOrWhiteSpace(connectionString))
            {
                ConnectionStringBuilder = new TaosConnectionStringBuilder(connectionString);
                ConnectionString = connectionString;
            }
            if (_dll_isloaded == false)
            {
                if (!string.IsNullOrEmpty(configdir) && !string.IsNullOrEmpty(configdir) && System.IO.File.Exists(Path.Combine(configdir, "taos.cfg")))
                {
                    TDengine.Options((int)TDengineInitOption.TSDB_OPTION_CONFIGDIR, configdir);
                }
               else  if (System.IO.File.Exists(Path.Combine(AppContext.BaseDirectory, "taos.cfg")))
                {
                    TDengine.Options((int)TDengineInitOption.TSDB_OPTION_CONFIGDIR, AppContext.BaseDirectory);
                }
                else if(System.IO.File.Exists(Path.Combine(Environment.CurrentDirectory, "taos.cfg")))
                {
                    TDengine.Options((int)TDengineInitOption.TSDB_OPTION_CONFIGDIR, Environment.CurrentDirectory);
                }
                else
                {
                    var configDir = "C:/TDengine/cfg";
#if NET5_0_OR_GREATER
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        configDir = "/etc/taos";

                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        configDir = "C:/TDengine/cfg";
                    }
#else
                configDir = "C:/TDengine/cfg";
#endif
                    var syscfg = new FileInfo(Path.Combine(configDir, "taos.cfg"));
                    if (syscfg.Exists)
                    {
                        TDengine.Options((int)TDengineInitOption.TSDB_OPTION_CONFIGDIR, configDir);
                    }
                }
                if (!string.IsNullOrEmpty(locale))
                {
                    TDengine.Options((int)TDengineInitOption.TSDB_OPTION_LOCALE, locale);
                }
                if (!string.IsNullOrEmpty(charset))
                {
                    TDengine.Options((int)TDengineInitOption.TSDB_OPTION_CHARSET, charset);
                }
                if (shell_activity_timer > 0)
                {
                    TDengine.Options((int)TDengineInitOption.TSDB_OPTION_SHELL_ACTIVITY_TIMER, $"{shell_activity_timer}");
                }
                TDengine.Init();
                Process.GetCurrentProcess().Disposed += (object sender, EventArgs e) =>
                    {
                        TDengine.Cleanup();
                    };
                _dll_isloaded = true;
            }
        }
        internal IntPtr TakeClient()
        {
            return _queue.Take();
        }
        internal  void ReturnClient(IntPtr  _taos)
        {
             _queue.Return(_taos);
        }
     



        /// <summary>
        ///     Gets or sets a string used to open the connection.
        /// </summary>
        /// <value>A string used to open the connection.</value>
        /// <seealso cref="TaosConnectionStringBuilder" />
        public override string ConnectionString
        {
            get => _connectionString;
            set
            {
                _connectionString = value;
                ConnectionStringBuilder = new TaosConnectionStringBuilder(value);
            }
        }

        internal TaosConnectionStringBuilder ConnectionStringBuilder { get; set; }

        public override int ConnectionTimeout => ConnectionStringBuilder.ConnectionTimeout;


        /// <summary>
        ///     Gets the path to the database file. Will be absolute for open connections.
        /// </summary>
        /// <value>The path to the database file.</value>
        public override string DataSource
        {
            get
            {
                string dataSource = null;

                return dataSource ?? ConnectionStringBuilder.DataSource;
            }
        }

        /// <summary>
        ///     Gets or sets the default <see cref="TaosCommand.CommandTimeout"/> value for commands created using
        ///     this connection. This is also used for internal commands in methods like
        ///     <see cref="BeginTransaction()"/>.
        /// </summary>
        /// <value>The default <see cref="TaosCommand.CommandTimeout"/> value</value>
        public virtual int DefaultTimeout { get; set; } = 60;


        string _version = string.Empty;
        /// <summary>
        ///     Gets the version of Taos used by the connection.
        /// </summary>
        /// <value>The version of Taos used by the connection.</value>
        public override string ServerVersion
        {
            get
            {
                var _taos = _queue.Take();
                if (_taos == IntPtr.Zero)
                {
                    _queue.Return(_taos);
                    TaosException.ThrowExceptionForRC(-10005, "Connection is not open", null);
                }
                else if (string.IsNullOrEmpty(_version))
                {
                    _version = Marshal.PtrToStringAnsi(TDengine.GetServerInfo(_taos));
                    _queue.Return(_taos);
                }
                return _version;
            }
        }
        public   string ClientVersion
        {
            get
            {
                if (string.IsNullOrEmpty(_version))
                {
                    _version = Marshal.PtrToStringAnsi(TDengine.GetClientInfo());
                }
                return _version;
            }
        }
        /// <summary>
        ///     Gets the current state of the connection.
        /// </summary>
        /// <value>The current state of the connection.</value>
        public override ConnectionState State
            => _state;

        /// <summary>
        ///     Gets the <see cref="DbProviderFactory" /> for this connection.
        /// </summary>
        /// <value>The <see cref="DbProviderFactory" />.</value>
        protected override DbProviderFactory DbProviderFactory
            => TaosFactory.Instance;

        /// <summary>
        ///     Gets or sets the transaction currently being used by the connection, or null if none.
        /// </summary>
        /// <value>The transaction currently being used by the connection.</value>
        protected internal virtual TaosTransaction Transaction { get; set; }

        public override string Database => ConnectionStringBuilder.DataBase;
        public  int  PoolSize => ConnectionStringBuilder.PoolSize;


        private void SetState(ConnectionState value)
        {
            var originalState = _state;
            if (originalState != value)
            {
                _state = value;
                OnStateChange(new StateChangeEventArgs(originalState, value));
            }
        }

        /// <summary>
        ///     Opens a connection to the database using the value of <see cref="ConnectionString" />. If
        ///     <c>Mode=ReadWriteCreate</c> is used (the default) the file is created, if it doesn't already exist.
        /// </summary>
        /// <exception cref="TaosException">A Taos error occurs while opening the connection.</exception>
        public override void Open()
        {
       
            if (State == ConnectionState.Open)
            {
                return;
            }
            if (!g_pool.ContainsKey(_connectionString))
            {
                g_pool.Add(_connectionString, new ConcurrentTaosQueue() {  Timeout= ConnectionTimeout});
            }
            _queue = g_pool[_connectionString];
            _queue.AddRef();
            if (ConnectionString == null)
            {
                throw new InvalidOperationException("Open Requires Set ConnectionString");
            }
            for (int i = 0; i < ConnectionStringBuilder.PoolSize+1; i++)
            {
                var c = TDengine.Connect(this.DataSource, ConnectionStringBuilder.Username, ConnectionStringBuilder.Password, "", (short)ConnectionStringBuilder.Port);
                if (c!=IntPtr.Zero)
                {
                    _queue.Return(c);
                }
            }
       
           if (_queue.TaosQueue.IsEmpty)
            {
                TaosException.ThrowExceptionForRC(new TaosErrorResult() {  Code=-1, Error= "Can't open  connection." });
            }
            else
            {
                SetState(ConnectionState.Open);
                this.ChangeDatabase(ConnectionStringBuilder.DataBase);
            }
        }
      
        /// <summary>
        ///     Closes the connection to the database. Open transactions are rolled back.
        /// </summary>
        public override void Close()
        {
            if (State != ConnectionState.Closed)
            {
                _queue.RemoveRef();
                if (_queue.GetRef() == 0)
                {
                    _queue.TaosQueue.ToList().ForEach(c =>
                    {
                        TDengine.Close(c);
                        }
                    );
                    _queue = null;
                    g_pool.Remove(_connectionString);
                }
            }
            Transaction?.Dispose();
            _nowdatabase = string.Empty;
            foreach (var reference in _commands)
            {
                if (reference.TryGetTarget(out var command))
                {
                    command.Dispose();
                }
            }
            _commands.Clear();
            SetState(ConnectionState.Closed);
        }

        /// <summary>
        ///     Releases any resources used by the connection and closes it.
        /// </summary>
        /// <param name="disposing">
        ///     true to release managed and unmanaged resources; false to release only unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        ///     Creates a new command associated with the connection.
        /// </summary>
        /// <returns>The new command.</returns>
        /// <remarks>
        ///     The command's <seealso cref="TaosCommand.Transaction" /> property will also be set to the current
        ///     transaction.
        /// </remarks>
        public new virtual TaosCommand CreateCommand()
            => new() { Connection = this, CommandTimeout = DefaultTimeout, Transaction = Transaction };
        public virtual TaosCommand CreateCommand(string commandtext)
          => new() { Connection = this, CommandText = commandtext, CommandTimeout = DefaultTimeout, Transaction = Transaction };

        /// <summary>
        ///     Creates a new command associated with the connection.
        /// </summary>
        /// <returns>The new command.</returns>
        protected override DbCommand CreateDbCommand()
            => CreateCommand();

        internal void AddCommand(TaosCommand command)
            => _commands.Add(new WeakReference<TaosCommand>(command));

        internal void RemoveCommand(TaosCommand command)
        {
            for (var i = _commands.Count - 1; i >= 0; i--)
            {
                if (!_commands[i].TryGetTarget(out var item)
                    || item == command)
                {
                    _commands.RemoveAt(i);
                }
            }
        }

        /// <summary>
        ///     Create custom collation.
        /// </summary>
        /// <param name="name">Name of the collation.</param>
        /// <param name="comparison">Method that compares two strings.</param>
        public virtual void CreateCollation(string name, Comparison<string> comparison)
            => CreateCollation(name, null, comparison != null ? (_, s1, s2) => comparison(s1, s2) : (Func<object, string, string, int>)null);

        /// <summary>
        ///     Create custom collation.
        /// </summary>
        /// <typeparam name="T">The type of the state object.</typeparam>
        /// <param name="name">Name of the collation.</param>
        /// <param name="state">State object passed to each invocation of the collation.</param>
        /// <param name="comparison">Method that compares two strings, using additional state.</param>
        public virtual void CreateCollation<T>(string name, T state, Func<T, string, string, int> comparison)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (State != ConnectionState.Open)
            {
                throw new InvalidOperationException($"CallRequiresOpenConnection{nameof(CreateCollation)}");
            }


        }

        /// <summary>
        ///     Begins a transaction on the connection.
        /// </summary>
        /// <returns>The transaction.</returns>
        public new virtual TaosTransaction BeginTransaction()
            => BeginTransaction(IsolationLevel.Unspecified);

        /// <summary>
        ///     Begins a transaction on the connection.
        /// </summary>
        /// <param name="isolationLevel">The isolation level of the transaction.</param>
        /// <returns>The transaction.</returns>
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            => BeginTransaction(isolationLevel);

        /// <summary>
        ///     Begins a transaction on the connection.
        /// </summary>
        /// <param name="isolationLevel">The isolation level of the transaction.</param>
        /// <returns>The transaction.</returns>
        public new virtual TaosTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            if (State != ConnectionState.Open)
            {
                throw new InvalidOperationException($"CallRequiresOpenConnection{nameof(BeginTransaction)}");
            }
            if (Transaction != null)
            {
                throw new InvalidOperationException($"ParallelTransactionsNotSupported");
            }

            return Transaction = new TaosTransaction(this, isolationLevel);
        }
        internal string _nowdatabase = string.Empty;

        internal bool SelectedDataBase => _nowdatabase != string.Empty ;
        /// <summary>
        ///     Changes the current database.  
        /// </summary>
        /// <param name="databaseName">The name of the database to use.</param>
        public override void ChangeDatabase(string databaseName)
        {
            var _sql = $"use {databaseName}";
            var ptr = _sql.ToUTF8IntPtr();
            var result=  _queue.TaosQueue.ToList().TrueForAll(_taos =>
            {
                var req = TDengine.Query(_taos, ptr.ptr);
                int code = TDengine.ErrorNo(req);
                var msg = TDengine.Error(req);
                TDengine.FreeResult(req);
                return code==0;
            });
            ptr.ptr.FreeUtf8IntPtr();
            if (result)
            {
                _nowdatabase = databaseName;
            }
        }

        public bool DatabaseExists(string databaseName)
        {
            var _sql = "SHOW DATABASES";
            var ds =  CreateCommand(_sql).ExecuteReader().ToObject<DatabaseSchema>();
            return (bool)(ds?.Any(d => d.name == databaseName));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lines">
        /// 示例:
        ///     "meters,location=Beijing.Haidian,groupid=2 current=11.8,voltage=221,phase=0.28 1648432611249",
        ///     "meters,location=Beijing.Haidian,groupid=2 current=13.4,voltage=223,phase=0.29 1648432611250",
        ///     "meters,location=Beijing.Haidian,groupid=3 current=10.8,voltage=223,phase=0.29 1648432611249",
        ///     "meters,location=Beijing.Haidian,groupid=3 current=11.3,voltage=221,phase=0.35 1648432611250"
        /// </param>
        /// <param name="precision"></param>
        /// <returns></returns>
        public int ExecuteBulkInsert(string[] lines, TDengineSchemalessPrecision precision = TDengineSchemalessPrecision.TSDB_SML_TIMESTAMP_MILLI_SECONDS) =>
            ExecuteBulkInsert(lines, TDengineSchemalessProtocol.TSDB_SML_LINE_PROTOCOL, precision);


        public int ExecuteBulkInsert<T>(IEnumerable<T> array, TDengineSchemalessPrecision precision = TDengineSchemalessPrecision.TSDB_SML_TIMESTAMP_MILLI_SECONDS)
        {
            var lines = array.Select(x => Newtonsoft.Json.JsonConvert.SerializeObject(x)).ToArray();
            return ExecuteBulkInsert(lines, TDengineSchemalessProtocol.TSDB_SML_JSON_PROTOCOL, precision);
        }

        public int ExecuteBulkInsert(JArray array, TDengineSchemalessPrecision precision = TDengineSchemalessPrecision.TSDB_SML_TIMESTAMP_MILLI_SECONDS)
        {
            var lines = array.Children().Select(x => x.ToString()).ToArray();
            return ExecuteBulkInsert(lines, TDengineSchemalessProtocol.TSDB_SML_JSON_PROTOCOL, precision);
        }
   

        private int ExecuteBulkInsert(string[] lines, TDengineSchemalessProtocol protocol, TDengineSchemalessPrecision precision)
        {
            int affectedRows = 0;
            var _taos= _queue.Take();
            IntPtr res = TDengine.SchemalessInsert(_taos, lines, lines.Length, (int)protocol, (int)precision);
         
            if (TDengine.ErrorNo(res) != 0)
            {
                var tdr = new TaosErrorResult() { Code = TDengine.ErrorNo(res), Error = TDengine.Error(res) };
                _queue.Return(_taos);
                TaosException.ThrowExceptionForRC(tdr);
            }
            else
            {
                affectedRows = TDengine.AffectRows(res);
                TDengine.FreeResult(res);
                _queue.Return(_taos);
            }
            return affectedRows;
        }
        private class AggregateContext<T>
        {
            public AggregateContext(T seed)
                => Accumulate = seed;

            public T Accumulate { get; set; }
            public Exception Exception { get; set; }
        }
    }
}
