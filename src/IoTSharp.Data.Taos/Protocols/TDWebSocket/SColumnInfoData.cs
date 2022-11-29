using System;
using TDengineDriver;

namespace IoTSharp.Data.Taos.Protocols.TDWebSocket
{
    //https://github.com/taosdata/TDengine/blob/186ff775eb37f463c12d604cd30d981180b158a2/include/common/tmsg.h#L684
    internal class SColumnInfo
    {
        public Int16 type;
        public Int32 bytes;
        public byte precision;
        public byte scale;
        /// <summary>
        /// #define IS_VAR_DATA_TYPE(t) \
        ///   (((t) == TSDB_DATA_TYPE_VARCHAR) || ((t) == TSDB_DATA_TYPE_NCHAR) || ((t) == TSDB_DATA_TYPE_JSON))
        /// </summary>
        /// <returns></returns>
        public bool IS_VAR_DATA_TYPE() => (((type) ==(short) TDengineDataType.TSDB_DATA_TYPE_VARCHAR) || ((type) == (short)TDengineDataType.TSDB_DATA_TYPE_NCHAR) || ((type) == (short)TDengineDataType.TSDB_DATA_TYPE_JSON));
    };
    internal class SVarColAttr
    {
        public Int32[] offset;    // start position for each entry in the list
        public Int32 length;    // used buffer size that contain the valid data
        public Int32 allocLen;  // allocated buffer size
    }

    //https://github.com/taosdata/TDengine/blob/e8a4bce2e7fee5064e15f4a4e0f47baa4e2a45de/include/common/tcommon.h#L227
    internal class SColumnInfoData
    {
        public SColumnInfoData()
        {
            info = new SColumnInfo();
            varmeta = new SVarColAttr();
        }
        public IntPtr pData;
        public int  pDataLen;
        // the corresponding block data in memory
        //        union {
        //char* nullbitmap;  // bitmap, one bit for each item in the list
        //      
        //    };
        public byte[] nullbitmap;  // bitmap, one bit for each item in the list
        public SVarColAttr varmeta;
        public SColumnInfo info;     // column info
        public bool hasNull;  // if current column data has null value.
    }

}