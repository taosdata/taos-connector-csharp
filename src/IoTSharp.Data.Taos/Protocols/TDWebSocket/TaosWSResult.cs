namespace IoTSharp.Data.Taos.Protocols.TDWebSocket
{

    public class TaosWSResult
    {
        public byte[] data { get; set; }
        public int block_length { get; set; }
        public WSQueryRsp meta { get; set; }
        public WSFetchRsp fetch { get; set; }
    }


}