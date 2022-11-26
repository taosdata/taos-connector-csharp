namespace IoTSharp.Data.Taos.Protocols.TDWebSocket
{
    public class WSActionRsp
    {
        /// <summary>
        /// 
        /// </summary>
        public int code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string message { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string action { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int req_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int timing { get; set; }
    }


}