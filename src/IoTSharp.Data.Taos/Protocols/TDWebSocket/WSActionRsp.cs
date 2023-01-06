namespace IoTSharp.Data.Taos.Protocols.TDWebSocket
{
    public class WSActionRsp
    {
        public int code { get; set; }
        public string message { get; set; }
        public string action { get; set; }
    }
}