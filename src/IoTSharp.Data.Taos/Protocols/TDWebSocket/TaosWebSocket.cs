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
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using TDengineDriver;

namespace IoTSharp.Data.Taos.Protocols.TDWebSocket
{

    internal partial class TaosWebSocket : ITaosProtocol
    {
        private ClientWebSocket _client = null;
        private string _databaseName;
        private TaosConnectionStringBuilder _builder;

        public bool ChangeDatabase(string databaseName)
        {
            _databaseName = databaseName;
            _builder.DataBase = _databaseName;
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
        private R WSExecute<R,T>(WSActionReq<T> req)
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
            return typeof(TaosWebSocket).Assembly.GetName().Version.ToString();
        }

        public string GetServerVersion()
        {
           var rep= WSExecute<WSVersionRsp,string> (new WSActionReq<string>() { Action = "version", Args="" });
            return rep.version;
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
            var url = $"ws://{builder.DataSource}:{builder.Port}/rest/ws";
            _client.ConnectAsync(new Uri(url), CancellationToken.None).Wait(TimeSpan.FromSeconds(builder.ConnectionTimeout));
            if (_client.State != WebSocketState.Open)
            {
                TaosException.ThrowExceptionForRC(new TaosErrorResult() { Code =(int) _client.CloseStatus, Error = _client .CloseStatusDescription});
            }
            var rep = WSExecute<WSConnRsp, WSConnReq>(new WSActionReq<WSConnReq> () {  Action = "conn", Args = new WSConnReq() {  user=builder.Username, password=builder.Password,  req_id=0} });
            if (rep.code!=0)
            {
                TaosException.ThrowExceptionForRC(new TaosErrorResult() { Code = rep.code, Error = rep.message });
            }
            return rep.code == 0;
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