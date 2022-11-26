using System.Collections.Generic;

namespace IoTSharp.Data.Taos.Protocols.TDWebSocket
{
    public class WSFetchRsp : WSActionRsp
    {
        public int req_id { get; set; }

        public int timing { get; set; }

        public int id { get; set; }

        public bool completed { get; set; }

        public List<int> lengths { get; set; }

        public int rows { get; set; }
    }


}