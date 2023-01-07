using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TDengineDriver;

namespace IoTSharp.Data.Taos.Driver
{
    internal static class TDengineExtensions
    {
        internal static string ToTypeName(this TDengineDataType DataType)
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
                case TDengineDataType.TSDB_DATA_TYPE_VARCHAR:
                    return "VARCHAR";
                case TDengineDataType.TSDB_DATA_TYPE_TIMESTAMP:
                    return "TIMESTAMP";
                case TDengineDataType.TSDB_DATA_TYPE_NCHAR:
                    return "NCHAR";
                case TDengineDataType.TSDB_DATA_TYPE_JSON:
                    return "JSON";
                case TDengineDataType.TSDB_DATA_TYPE_VARBINARY:
                    return "VARBINARY";
                case TDengineDataType.TSDB_DATA_TYPE_DECIMAL:
                    return "DECIMAL";
                case TDengineDataType.TSDB_DATA_TYPE_MEDIUMBLOB:
                    return "MEDIUMBLOB";
                default:
                    return "undefine";
            }
        }

        internal static Type ToCrlType(this TDengineDataType DataType)
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
                case TDengineDataType.TSDB_DATA_TYPE_VARCHAR:
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
}
