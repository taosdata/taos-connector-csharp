using System;
using TDengineDriver;

namespace IoTSharp.Data.Taos.Protocols.TDWebSocket
{
    internal class WSDataRow
    {
        public WSDataRow(TDengineDataType type, IntPtr data, short len, int  precision)
        {
            this.type = type;
            this.data = data;
            this.len = len;
            this.precision = (TSDB_TIME_PRECISION)precision;
        }
         public TSDB_TIME_PRECISION precision { get; set; }
        public TDengineDataType type { get; set; }
        public IntPtr data { get; set; }
        public short len { get; set; }
 
    }
}
