namespace IoTSharp.Data.Taos.Protocols.TDWebSocket
{

    public class WSQueryReq
    {
        public long req_id { get; set; }
        public string sql { get; set; }
    }


}