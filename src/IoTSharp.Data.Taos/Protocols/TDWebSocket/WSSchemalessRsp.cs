namespace IoTSharp.Data.Taos.Protocols.TDWebSocket
{
    public class WSSchemalessRsp
    {
        public int req_id { get; set; }
        public string action { get; set; }
        public int timing { get; set; }
    }
}