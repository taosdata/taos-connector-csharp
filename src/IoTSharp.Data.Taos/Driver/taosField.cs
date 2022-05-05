using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TDengineDriver;

namespace IoTSharp.Data.Taos.Driver
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1, Size = 68)]
    public struct taosField
    {
        public taosField()
        {
            _name = new byte[65];
            type = 0;
            size = 0;
        }
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 65)]
        public byte[] _name;
        [MarshalAs(UnmanagedType.U1, SizeConst = 1)]
        public byte type;
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public short size;

        public string Name => Encoding.UTF8.GetString(_name, 0, _name.AsSpan().IndexOf((byte)0));

        public TDengineDataType DataType => (TDengineDataType)type;
        public Type CrlType
        {
            get
            {
                Type crltype = typeof(DBNull);
                switch (DataType)
                {
                    case TDengineDataType.TSDB_DATA_TYPE_BOOL:
                        crltype = typeof(bool);
                        break;
                    case TDengineDataType.TSDB_DATA_TYPE_TINYINT:
                        crltype = typeof(sbyte);
                        break;
                    case TDengineDataType.TSDB_DATA_TYPE_UTINYINT:
                        crltype = typeof(byte);
                        break;
                    case TDengineDataType.TSDB_DATA_TYPE_SMALLINT:
                        crltype = typeof(short);
                        break;
                    case TDengineDataType.TSDB_DATA_TYPE_USMALLINT:
                        crltype = typeof(ushort);
                        break;
                    case TDengineDataType.TSDB_DATA_TYPE_INT:
                        crltype = typeof(int);
                        break;
                    case TDengineDataType.TSDB_DATA_TYPE_UINT:
                        crltype = typeof(uint);
                        break;
                    case TDengineDataType.TSDB_DATA_TYPE_BIGINT:
                        crltype = typeof(long);
                        break;
                    case TDengineDataType.TSDB_DATA_TYPE_UBIGINT:
                        crltype = typeof(ulong);
                        break;
                    case TDengineDataType.TSDB_DATA_TYPE_FLOAT:
                        crltype = typeof(float);
                        break;
                    case TDengineDataType.TSDB_DATA_TYPE_DOUBLE:
                        crltype = typeof(double);
                        break;
                    case TDengineDataType.TSDB_DATA_TYPE_BINARY:
                        crltype = typeof(string);
                        break;
                    case TDengineDataType.TSDB_DATA_TYPE_TIMESTAMP:
                        crltype = typeof(DateTime);
                        break;
                    case TDengineDataType.TSDB_DATA_TYPE_NCHAR:
                        crltype = typeof(string);
                        break;
                }
                return crltype;
            }
        }

        public string TypeName
        {
            get
            {
                switch (DataType)
                {
                    case TDengineDataType.TSDB_DATA_TYPE_BOOL:
                        return "BOOL";
                    case TDengineDataType.TSDB_DATA_TYPE_TINYINT:
                        return "TINYINT";
                    case TDengineDataType.TSDB_DATA_TYPE_SMALLINT:
                        return "SMALLINT";
                    case TDengineDataType.TSDB_DATA_TYPE_INT:
                        return "INT";
                    case TDengineDataType.TSDB_DATA_TYPE_BIGINT:
                        return "BIGINT";
                    case TDengineDataType.TSDB_DATA_TYPE_UTINYINT:
                        return "TINYINT UNSIGNED";
                    case TDengineDataType.TSDB_DATA_TYPE_USMALLINT:
                        return "SMALLINT UNSIGNED";
                    case TDengineDataType.TSDB_DATA_TYPE_UINT:
                        return "INT UNSIGNED";
                    case TDengineDataType.TSDB_DATA_TYPE_UBIGINT:
                        return "BIGINT UNSIGNED";
                    case TDengineDataType.TSDB_DATA_TYPE_FLOAT:
                        return "FLOAT";
                    case TDengineDataType.TSDB_DATA_TYPE_DOUBLE:
                        return "DOUBLE";
                    case TDengineDataType.TSDB_DATA_TYPE_BINARY:
                        return "BINARY";
                    case TDengineDataType.TSDB_DATA_TYPE_TIMESTAMP:
                        return "TIMESTAMP";
                    case TDengineDataType.TSDB_DATA_TYPE_NCHAR:
                        return "NCHAR";
                    case TDengineDataType.TSDB_DATA_TYPE_JSONTAG:
                        return "JSON";
                    default:
                        return "undefine";
                }
            }
        }
    }
}
