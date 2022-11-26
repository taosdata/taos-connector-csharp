namespace IoTSharp.Data.Taos.Protocols.TDWebSocket
{
    public class WSActionReq<T>
    {
        /// <summary>
        /// 
        /// </summary>
        public string action { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public T args { get; set; }
    }


}