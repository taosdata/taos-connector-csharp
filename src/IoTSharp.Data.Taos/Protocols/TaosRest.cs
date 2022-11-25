using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTSharp.Data.Taos.Protocols
{
    internal class TaosRest
    {
        //        public new virtual TaosDataReader ExecuteReader(CommandBehavior behavior, RestClient rest)
        //        {
        //            if ((behavior & ~(CommandBehavior.Default | CommandBehavior.SequentialAccess | CommandBehavior.SingleResult
        //                              | CommandBehavior.SingleRow | CommandBehavior.CloseConnection)) != 0)
        //            {
        //                throw new ArgumentException($"InvalidCommandBehavior{behavior}");
        //            }

        //            if (DataReader != null)
        //            {
        //                throw new InvalidOperationException($"DataReaderOpen");
        //            }

        //            if (_connection?.State != ConnectionState.Open)
        //            {
        //                _connection.Open();
        //                if (_connection?.State != ConnectionState.Open)
        //                {
        //                    throw new InvalidOperationException($"CallRequiresOpenConnection{nameof(ExecuteReader)}");
        //                }
        //            }

        //            if (string.IsNullOrEmpty(_commandText))
        //            {
        //                throw new InvalidOperationException($"CallRequiresSetCommandText{nameof(ExecuteReader)}");
        //            }

        //            if (Transaction != _connection.Transaction)
        //            {
        //                throw new InvalidOperationException(
        //                    Transaction == null
        //                        ? "TransactionRequired"
        //                        : "TransactionConnectionMismatch");
        //            }
        //            if (_connection.Transaction?.ExternalRollback == true)
        //            {
        //                throw new InvalidOperationException("TransactionCompleted");
        //            }
        //            int rc;
        //            var unprepared = false;
        //            TaosDataReader dataReader = null;
        //            var closeConnection = (behavior & CommandBehavior.CloseConnection) != 0;
        //            try
        //            {
        //#if DEBUG
        //                Console.WriteLine($"_commandText:{_commandText}");
        //#endif

        //#if NET46
        //                var request = new RestRequest(Method.POST);
        //#else
        //                  var request = new RestRequest(Method.Post);
        //#endif
        //                request.AddHeader("User-Agent", "Maikebing.Data.Taos/0.0.1");

        //                request.AddHeader("Authorization", $"Basic {Connection.Token}");
        //                request.AddHeader("Content-Type", "text/plain");
        //                request.AddParameter( "undefined", _commandText, "application/json", ParameterType.RequestBody);
        //                RestResponse response = rest.Execute(request);
        //                if (response.StatusCode == System.Net.HttpStatusCode.OK)
        //                {
        //                    var tr = Newtonsoft.Json.JsonConvert.DeserializeObject<TaosResult>(response.Content);
        //#if DEBUG
        //                    Console.WriteLine($"Exec {tr.status},rows:{tr.rows},cols:{tr.head?.Count}");
        //#endif
        //                    dataReader = new TaosDataReader(this, tr, closeConnection);
        //                }
        //                else if (string.IsNullOrEmpty(response.Content))
        //                {
        //                    TaosException.ThrowExceptionForRC(_commandText, new TaosErrorResult() { status = response.StatusCode.ToString(), code = -9999, desc = "Server is not available" });
        //                }
        //                else
        //                {
        //                    var tr = Newtonsoft.Json.JsonConvert.DeserializeObject<TaosErrorResult>(response.Content);
        //#if DEBUG
        //                    Console.WriteLine($"Exec {tr.status},code:{tr.code},message:{tr.desc}");
        //#endif
        //                    TaosException.ThrowExceptionForRC(_commandText, tr);
        //                }
        //            }
        //            catch when (unprepared)
        //            {
        //                throw;
        //            }

        //            return dataReader;
        //        }

    }
}
