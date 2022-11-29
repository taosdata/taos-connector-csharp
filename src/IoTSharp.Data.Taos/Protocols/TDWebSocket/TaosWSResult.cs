namespace IoTSharp.Data.Taos.Protocols.TDWebSocket
{

    public class TaosWSResult
    {
        public byte[] data { get; set; }
        public WSQueryRsp meta { get; set; }
        public int rows { get;  set; }
    }


}