using Newtonsoft.Json;

namespace IoTSharp.Data.Taos.Protocols.TDWebSocket
{
 
    public class WSActionReq<T>
    {
        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("args")]
        public T Args { get; set; }
    }


}