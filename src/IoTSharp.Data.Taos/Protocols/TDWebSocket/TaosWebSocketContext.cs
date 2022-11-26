using IoTSharp.Data.Taos.Driver;
using IoTSharp.Data.Taos.Protocols.TDRESTful;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TDengineDriver;

namespace IoTSharp.Data.Taos.Protocols.TDWebSocket
{
    internal class TaosWebSocketContext : ITaosContext
    {

        public int AffectRows { get; set; }
        public int FieldCount { get; set; }
        private IntPtr rowdata;

        public bool CloseConnection { get; set; }

        public TaosWebSocketContext(IntPtr ptr)
        {
            rowdata = ptr;
            AffectRows = tr.rows;
            FieldCount = tr.column_meta.Count;
          
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public bool GetBoolean(int ordinal)
        {
            throw new NotImplementedException();
        }

        public byte GetByte(int ordinal)
        {
            throw new NotImplementedException();
        }

        public long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public DateTime GetDataTime(int ordinal)
        {
            throw new NotImplementedException();
        }

        public int GetErrorNo()
        {
            throw new NotImplementedException();
        }

        public int GetFieldSize(int ordinal)
        {
            throw new NotImplementedException();
        }

        public Type GetFieldType(int ordinal)
        {
            throw new NotImplementedException();
        }

        public string GetName(int ordinal)
        {
            throw new NotImplementedException();
        }

        public int GetOrdinal(string name)
        {
            throw new NotImplementedException();
        }

        public Stream GetStream(int ordinal)
        {
            throw new NotImplementedException();
        }

        public TimeSpan GetTimeSpan(int ordinal)
        {
            throw new NotImplementedException();
        }

        public object GetValue(int ordinal)
        {
            throw new NotImplementedException();
        }

        public TaosException LastException()
        {
            throw new NotImplementedException();
        }

        public bool Read()
        {
            throw new NotImplementedException();
        }
    }
}