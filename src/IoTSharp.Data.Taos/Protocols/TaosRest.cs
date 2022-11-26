using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace IoTSharp.Data.Taos.Protocols
{
    internal class TaosRest : ITaosProtocol
    {
        private string _token;
        private RestClient _client=null;
        private string _databaseName;

        public bool ChangeDatabase(string databaseName)
        {
            _databaseName = databaseName;
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
#if DEBUG
                Console.WriteLine($"_commandText:{_commandText}");
#endif
                var body = _commandText;
#if NET46
                var request = new RestRequest();
      
                request.AddParameter("",body, "text/plain",  ParameterType.RequestBody);
#else
                var request = new RestRequest("",Method.Post);
                request.AddHeader("User-Agent", "Maikebing.Data.Taos/0.0.1");
                request.AddHeader("Content-Type", "text/plain");
                request.AddParameter("", body,  ParameterType.RequestBody);
#endif
                request.AddHeader("User-Agent", "Maikebing.Data.Taos/0.0.1");
                request.AddHeader("Content-Type", "text/plain");
#if NET46
                var response = _client.Execute(request, Method.POST);
#else
                var response = _client.Execute(request, Method.Post);
#endif
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var tr = Newtonsoft.Json.JsonConvert.DeserializeObject<TaosResult>(response.Content);
#if DEBUG
                    Console.WriteLine($"Exec {tr.status},rows:{tr.rows},cols:{tr.head?.Count}");
#endif
                    dataReader = new TaosDataReader(command, new TaosRestContext(tr));
                }
                else if (string.IsNullOrEmpty(response.Content))
                {
                    TaosException.ThrowExceptionForRC(_commandText,  new Taos.TaosErrorResult() {  Code =(int) response.StatusCode,  Error=response.ErrorMessage });
                }
                else
                {
                    var tr = Newtonsoft.Json.JsonConvert.DeserializeObject<RestTaosErrorResult>(response.Content);
#if DEBUG
                    Console.WriteLine($"Exec {tr.status},code:{tr.code},message:{tr.desc}");
#endif
                    TaosException.ThrowExceptionForRC(_commandText,  new Taos.TaosErrorResult() { Code=tr.code, Error=tr.desc });
                }
            }
            catch when (unprepared)
            {
                throw;
            }

            return dataReader;

        }

        public string GetClientVersion()
        {
            //SELECT CLIENT_VERSION();
            return "";
        }

        public string GetServerVersion()
        {
            //SELECT SERVER_VERSION();
            return "";
        }

        public void InitTaos(string configdir, int shell_activity_timer, string locale, string charset)
        {
        
        }

        public bool Open(TaosConnectionStringBuilder connectionStringBuilder)
        {
            var builder = connectionStringBuilder;
            _token = Convert.ToBase64String(Encoding.Default.GetBytes($"{builder.Username}:{builder.Password}"));
            string _timez = string.IsNullOrEmpty(builder.TimeZone) ? "" : $"?tz={builder.TimeZone}";
#if NET46
            _client = new RestClient($"http://{builder.DataSource}:{builder.Port}/rest/sql/{builder.DataBase}{_timez}");
            _client.Timeout = builder.ConnectionTimeout;
            _client.Authenticator = new HttpBasicAuthenticator(builder.Username, builder.Password);
#else
            _client = new RestClient($"http://{builder.DataSource}:{builder.Port}/rest/sql/{builder.DataBase}{_timez}");
            _client.Options.MaxTimeout = builder.ConnectionTimeout;
            _client.UseAuthenticator(new HttpBasicAuthenticator(builder.Username, builder.Password));
#endif

            return true;
        }

        public void Return(nint taos)
        {
      
        }

        public nint Take()
        {
            return IntPtr.Zero;
        }
    }
    public class RestTaosErrorResult
    {
        /// <summary>
        /// 
        /// </summary>
        public string status { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string desc { get; set; }
    }
    public class TaosResult
    {
 
        public string status { get; set; }
  
        public List<string> head { get; set; }
 
        public object data { get; set; }
 
        public int rows { get; set; }
    }
}
