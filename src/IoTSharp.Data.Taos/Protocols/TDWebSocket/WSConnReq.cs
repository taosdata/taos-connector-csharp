namespace IoTSharp.Data.Taos.Protocols.TDWebSocket
{

    public class WSConnReq
    {
        public int req_id { get; set; }
        public string user { get; set; }
        public string password { get; set; }
        public string db { get; set; }
    }


}