using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using TDengineDriver;

namespace IoTSharp.Data.Taos.Protocols
{
    internal class TaosRestContext : ITaosContext
    {
        private readonly TaosResult tr;

        public int AffectRows { get; set; }
        public int FieldCount { get; set; }

        

        public bool CloseConnection { get; set; }

        public TaosRestContext(TaosResult tr)
        {
            this.tr = tr;
            AffectRows = tr.rows;
            FieldCount = tr.head.Count;
        }

        public bool Read()
        {
            return true;
        }

        public TaosException LastException()
        {
            return new TaosException(new TaosErrorResult() { Code = 0, Error = "" });
        }

        public int GetErrorNo()
        {
            return 0;
        }

        public void Dispose()
        {
         
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

        public DataTable GetSchemaTable(Func<int, string> getName, Func<int, Type> getFieldType, Func<int, string> getDataTypeName, TaosCommand _command)
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
    }
}
