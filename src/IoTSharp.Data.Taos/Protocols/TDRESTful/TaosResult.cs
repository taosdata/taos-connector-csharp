using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace IoTSharp.Data.Taos.Protocols.TDRESTful
{
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