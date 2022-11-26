using System;
using System.Data;
using System.IO;

namespace IoTSharp.Data.Taos.Protocols
{
    interface ITaosContext
    {
        int AffectRows { get; set; }
        bool CloseConnection { get; set; }
        int FieldCount { get; set; }

        void Dispose();
        bool GetBoolean(int ordinal);
        byte GetByte(int ordinal);
        long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length);
        DateTime GetDataTime(int ordinal);
        int GetErrorNo();
        Type GetFieldType(int ordinal);
        string GetName(int ordinal);
        int GetOrdinal(string name);
        DataTable GetSchemaTable(Func<int, string> getName, Func<int, Type> getFieldType, Func<int, string> getDataTypeName, TaosCommand _command);
        Stream GetStream(int ordinal);
        TimeSpan GetTimeSpan(int ordinal);
        object GetValue(int ordinal);
        TaosException LastException();
        bool Read();
    }
}
