using IoTSharp.Data.Taos.Protocols.TDRESTful;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using TDengineDriver;

namespace IoTSharp.Data.Taos.Protocols.TDWebSocket
{
    public class WSActionRsp
    {
        /// <summary>
        /// 
        /// </summary>
        public int code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string message { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string action { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int req_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int timing { get; set; }
    }

    public class conn
    {
        /// <summary>
        /// 
        /// </summary>
        public int req_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string user { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string password { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string db { get; set; }
    }

    public class WSActionReq<T>
    {
        /// <summary>
        /// 
        /// </summary>
        public string action { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public T args { get; set; }
    }

    internal class TaosWebSocket : ITaosProtocol
    {
        private ClientWebSocket _client = null;
        private string _databaseName;
        private TaosConnectionStringBuilder _builder;

        public bool ChangeDatabase(string databaseName)
        {
            _databaseName = databaseName;
            _builder.DataBase = _databaseName;
            ResetClient(_builder);
            return true;
        }

        public void Close(TaosConnectionStringBuilder connectionStringBuilder)
        {
#if NET46

#else
            _client?.Dispose();
#endif
        }

        public TaosDataReader ExecuteReader(CommandBehavior behavior, TaosCommand command)
        {
            var _commandText = command._commandText;
            var _connection = command._connection;
            var _parameters = command._parameters;
            if ((behavior & ~(CommandBehavior.Default | CommandBehavior.SequentialAccess | CommandBehavior.SingleResult
                              | CommandBehavior.SingleRow | CommandBehavior.CloseConnection)) != 0)
            {
                throw new ArgumentException($"InvalidCommandBehavior{behavior}");
            }

            if (_connection?.State != ConnectionState.Open)
            {
                _connection.Open();
                if (_connection?.State != ConnectionState.Open)
                {
                    throw new InvalidOperationException($"CallRequiresOpenConnection{nameof(ExecuteReader)}");
                }
            }

            if (string.IsNullOrEmpty(_commandText))
            {
                throw new InvalidOperationException($"CallRequiresSetCommandText{nameof(ExecuteReader)}");
            }
            var unprepared = false;
            TaosDataReader dataReader = null;
            var closeConnection = (behavior & CommandBehavior.CloseConnection) != 0;
            try
            {
                var tr = Execute(_commandText);
                dataReader = new TaosDataReader(command, new TaosWebSocketContext(tr));
            }
            catch when (unprepared)
            {
                throw;
            }
            return dataReader;
        }
        private R Execute<T, R>(WSActionReq<T> req)
        {
            var _req = Newtonsoft.Json.JsonConvert.SerializeObject(req);
            Debug.WriteLine(_req);
            var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(_req));
            _client.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None).Wait(TimeSpan.FromSeconds(_builder.ConnectionTimeout));
            ArraySegment<byte> bytes = new ArraySegment<byte>(new byte[4 * 1024 * 1024]);
            var result = _client.ReceiveAsync(bytes, CancellationToken.None).GetAwaiter().GetResult();
            var json = Encoding.UTF8.GetString(bytes.Array, 0, result.Count);
            Debug.WriteLine(json);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<R>(json);
        }
        private TaosResult Execute(string _commandText)
        {

            return null;
        }

        public string GetClientVersion()
        {
            return Execute("SELECT CLIENT_VERSION()")?.Scalar as string;
        }

        public string GetServerVersion()
        {
            return Execute("SELECT SERVER_VERSION()")?.Scalar as string;
        }

        public void InitTaos(string configdir, int shell_activity_timer, string locale, string charset)
        {
        }

        public bool Open(TaosConnectionStringBuilder connectionStringBuilder)
        {
            _builder = connectionStringBuilder;
            var builder = connectionStringBuilder;
            string _timez = string.IsNullOrEmpty(builder.TimeZone) ? "" : $"?tz={builder.TimeZone}";
            _client = new ClientWebSocket();
            _client.Options.Credentials = new NetworkCredential(builder.Username, builder.Password);
            var url = $"http://{builder.DataSource}:{builder.Port}/rest/ws";
            _client.ConnectAsync(new Uri(url), CancellationToken.None).Wait(TimeSpan.FromSeconds(builder.ConnectionTimeout));
            if (_client.State != WebSocketState.Open)
            {
                throw new Exception($"{_client.State}");
            }

            return true;
        }

        private void ResetClient(TaosConnectionStringBuilder connectionStringBuilder)
        {

        }

        public void Return(nint taos)
        {
        }

        public nint Take()
        {
            return IntPtr.Zero;
        }

        public int ExecuteBulkInsert(string[] lines, TDengineSchemalessProtocol protocol, TDengineSchemalessPrecision precision)
        {
            throw new NotSupportedException("RESTful  不支持 ExecuteBulkInsert");
        }
    }


}