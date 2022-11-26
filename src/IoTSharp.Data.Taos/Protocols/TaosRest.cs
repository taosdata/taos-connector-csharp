using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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
                var tr = Execute( _commandText);
                dataReader = new TaosDataReader(command, new TaosRestContext(tr));
            }
            catch when (unprepared)
            {
                throw;
            }
            return dataReader;

        }

        private TaosResult Execute(string _commandText)
        {
            TaosResult result = null;
#if DEBUG
            Console.WriteLine($"_commandText:{_commandText}");
#endif
            var body = _commandText;
#if NET46
                var request = new RestRequest();
      
                request.AddParameter("",body, "text/plain",  ParameterType.RequestBody);
#else
            var request = new RestRequest("", Method.Post);
            request.AddHeader("User-Agent", "Maikebing.Data.Taos/0.0.1");
            request.AddHeader("Content-Type", "text/plain");
            request.AddParameter("", body, ParameterType.RequestBody);
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
                result = Newtonsoft.Json.JsonConvert.DeserializeObject<TaosResult>(response.Content);
#if DEBUG
                Console.WriteLine($"Exec code {result.code},rows:{result.rows},cols:{result.column_meta?.Count}");
#endif
                if (result.code != 0)
                {
                    TaosException.ThrowExceptionForRC(_commandText, new Taos.TaosErrorResult() { Code = result.code, Error = result.desc });
                }
            }
            else if (string.IsNullOrEmpty(response.Content))
            {
                TaosException.ThrowExceptionForRC(_commandText, new Taos.TaosErrorResult() { Code = (int)response.StatusCode, Error = response.ErrorMessage });
            }
            else
            {
                var tr = Newtonsoft.Json.JsonConvert.DeserializeObject<TaosResult>(response.Content);
#if DEBUG
                Console.WriteLine($"Exec code:{tr.code},message:{tr.desc}");
#endif
                TaosException.ThrowExceptionForRC(_commandText, new Taos.TaosErrorResult() { Code = tr.code, Error = tr.desc });
            }
            return result;
        }

        public string GetClientVersion()
        {
           return  Execute("SELECT CLIENT_VERSION()")?.Scalar as string;
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
            ResetClient(_builder);
            return true;
        }

        private void ResetClient(TaosConnectionStringBuilder connectionStringBuilder)
        {
            var builder = connectionStringBuilder;
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
        }

        public void Return(nint taos)
        {
      
        }

        public nint Take()
        {
            return IntPtr.Zero;
        }
    }
  
    public class TaosResult
    {
 
        public int code { get; set; }
        public string desc { get; set; }
 
        public List<List<string>> column_meta { get; set; }
  
        public JArray data { get; set; }

        public int rows { get; set; }
        
        public object Scalar => (data?.First?.First as JValue)?.Value;
    }
}
