namespace IoTSharp.Data.Taos.Protocols.TDWebSocket
{
    public class WSSchemalessRsp: WSActionRsp
    {
        public int req_id { get; set; }
        public int timing { get; set; }

    }
}