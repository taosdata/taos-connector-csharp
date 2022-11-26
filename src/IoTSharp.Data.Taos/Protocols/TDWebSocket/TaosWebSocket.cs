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
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
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
        private R WSExecute<R,T>(WSActionReq<T> req,Action<byte[],int>  action=null, int buffer_lenght=4*1024*1024)
        {
            R result = default;
            var _req = Newtonsoft.Json.JsonConvert.SerializeObject(req);
            Debug.WriteLine(_req);
            var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(_req));
            _client.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None).Wait(TimeSpan.FromSeconds(_builder.ConnectionTimeout));
            ArraySegment<byte> bytes = new ArraySegment<byte>(new byte[buffer_lenght]);
            var wresult = _client.ReceiveAsync(bytes, CancellationToken.None).GetAwaiter().GetResult();
            action?.Invoke(bytes.Array, wresult.Count);
            var json = Encoding.UTF8.GetString(bytes.Array, 0, wresult.Count);
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<R>(json);
            return result;
        }
        //https://github.com/taosdata/taosadapter/blob/e57b466a3f243901bc93b15519b57a26d649612a/controller/rest/ws_test.go
        //https://github.com/taosdata/taosadapter/blob/e57b466a3f243901bc93b15519b57a26d649612a/controller/rest/ws.go#L152
        private volatile static int  _reqid=0;
        private TaosResult Execute(string _commandText)
        {
            var dt = DateTime.Now;
            _reqid++;
            if (_reqid > 99) _reqid = 0;
            long reqid = (long)(dt.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds * 100 + _reqid);
            var repquery = WSExecute<WSQueryRsp, WSQueryReq>(new WSActionReq<WSQueryReq>() { Action = "query", Args = new WSQueryReq() { req_id = reqid, sql = _commandText } });
            var repfetch = WSExecute<WSFetchRsp, WSFetchReq>(new WSActionReq<WSFetchReq>() { Action = "fetch", Args = new WSFetchReq { req_id = repquery.req_id } });
            foreach (var _block_length in repfetch.lengths)
            {
                byte[] buffer = new byte[_block_length];
                var repfetch_block = WSExecute<byte[], WSFetchReq>(
           new WSActionReq<WSFetchReq>() { Action = "fetch_block", Args = new WSFetchReq { req_id = repquery.req_id } }
           , (byte[] bytes, int len) => Array.Copy(bytes, buffer, len), _block_length);
                IntPtr ptr = Marshal.AllocHGlobal(_block_length);
                Marshal.Copy(buffer, 0, ptr, _block_length);
                var id = Marshal.ReadInt64(ptr);
                //*(*uintptr)(unsafe.Pointer(&data)) +uintptr(8)
                //    result:= parser.ReadBlock(unsafe.Pointer(columnPtr), rows, colTypes, precision)
                var result = IntPtr.Add(ptr, 8);
            }
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