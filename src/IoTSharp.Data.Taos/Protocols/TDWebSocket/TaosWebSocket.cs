using IoTSharp.Data.Taos.Protocols.TDRESTful;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
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
        private R WSExecute<R, T>(WSActionReq<T> req, Action<byte[], int> action = null)
        {
            R _result = default;
            var token = CancellationToken.None;
            var _req = Newtonsoft.Json.JsonConvert.SerializeObject(req);
            Debug.WriteLine(_req);
            _client.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(_req)), WebSocketMessageType.Text, true, CancellationToken.None).Wait(TimeSpan.FromSeconds(_builder.ConnectionTimeout));
            int bufferSize = 1024*1024*4;
            var buffer = new byte[bufferSize];
            var offset = 0;
            var free = buffer.Length;
                    WebSocketMessageType _msgType;
            while (true)
            {
                var result = _client.ReceiveAsync(new ArraySegment<byte>(buffer, offset, free), token).GetAwaiter().GetResult();
                offset += result.Count;
                free -= result.Count;
                if (result.EndOfMessage)
                {
                    _msgType = result.MessageType;
                    break;
                }
                if (free == 0)
                {
                    // No free space
                    // Resize the outgoing buffer
                    var newSize = buffer.Length + bufferSize;
                    // Check if the new size exceeds a limit
                    // It should suit the data it receives
                    // This limit however has a max value of 2 billion bytes (2 GB)
                    if (newSize > 1024*1024*1024)
                    {
                        throw new Exception("Maximum size exceeded");
                    }
                    var newBuffer = new byte[newSize];
                    Array.Copy(buffer, 0, newBuffer, 0, offset);
                    buffer = newBuffer;
                    free = buffer.Length - offset;
                }
            }

  
            switch (_msgType)
            {
                case WebSocketMessageType.Binary:
                    action?.Invoke(buffer, offset);
                    break;
                case WebSocketMessageType.Close:
                    break;
                case WebSocketMessageType.Text:
                    var json = Encoding.UTF8.GetString(buffer,0, offset);
                    _result = Newtonsoft.Json.JsonConvert.DeserializeObject<R>(json);
                    break;
                default:
                    break;
            }
            return _result;
        }
        //https://github.com/taosdata/taosadapter/blob/e57b466a3f243901bc93b15519b57a26d649612a/controller/rest/ws_test.go
        //https://github.com/taosdata/taosadapter/blob/e57b466a3f243901bc93b15519b57a26d649612a/controller/rest/ws.go#L152
        private volatile static int  _reqid=0;
        private TaosWSResult Execute(string _commandText)
        {
            var dt = DateTime.Now;
            TaosWSResult wSResult = new TaosWSResult(); ;
            _reqid++;
            if (_reqid > 99999) _reqid = 0;
            var repquery = WSExecute<WSQueryRsp, WSQueryReq>(new WSActionReq<WSQueryReq>() { Action = "query", Args = new WSQueryReq() { req_id = _reqid, sql = _commandText } });
            if (repquery.code != 0)
            {
                TaosException.ThrowExceptionForRC(new TaosErrorResult() { Code = repquery.code, Error = repquery.message });
            }
            if (!repquery.is_update)
            {
                var repfetch = WSExecute<WSFetchRsp, WSFetchReq>(new WSActionReq<WSFetchReq>() { Action = "fetch", Args = new WSFetchReq { req_id = repquery.req_id, id = repquery.id } });
                if (repfetch.code == 0)
                {
                    List<byte> data = new List<byte>();
                    int _rows = repfetch.rows;
                    do
                    {
                        byte[] buffer = new byte[] { };
                        var repfetch_block = WSExecute<byte[], WSFetchReq>
                           (
                               new WSActionReq<WSFetchReq>()
                               {
                                   Action = "fetch_block",
                                   Args = new WSFetchReq() { req_id = repquery.req_id, id = repfetch.id }
                               },
                               (byte[] bytes, int len) =>
                               {
                                   buffer = new byte[len];
                                   Array.Copy(bytes, buffer, len);
                               }
                         );
                        repfetch = WSExecute<WSFetchRsp, WSFetchReq>(new WSActionReq<WSFetchReq>() { Action = "fetch", Args = new WSFetchReq { req_id = repquery.req_id, id = repquery.id } });
                        _rows += repfetch.rows;
                        data.AddRange(buffer);
                    } while (!repfetch.completed);
                    var free_result = WSExecute<WSFetchRsp, WSFetchReq>(new WSActionReq<WSFetchReq>() { Action = "free_result", Args = new WSFetchReq { req_id = repquery.req_id, id = repquery.id } });
                    wSResult = new TaosWSResult() { data = data.ToArray(), meta = repquery, rows = _rows };
                }
                else
                {
                    TaosException.ThrowExceptionForRC(new TaosErrorResult() { Code = repfetch.code, Error = repfetch.message });
                }
            }
            else
            {
                wSResult = new TaosWSResult() { meta = repquery };
            }
            return wSResult;
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