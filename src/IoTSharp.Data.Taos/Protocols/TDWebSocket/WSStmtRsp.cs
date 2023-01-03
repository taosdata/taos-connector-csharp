namespace IoTSharp.Data.Taos.Protocols.TDWebSocket
{
    public class WSStmtRsp : WSActionRsp
    {
        public int req_id { get; set; }
        public int timing { get; set; }
        public int stmt_id { get; set; }
    }
    public class WSStmtExecRsp : WSStmtRsp
    {
        public int affected { get; set; }
    }
}