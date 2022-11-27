using IoTSharp.Data.Taos.Driver;
using IoTSharp.Data.Taos.Protocols.TDRESTful;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using TDengineDriver;

namespace IoTSharp.Data.Taos.Protocols.TDWebSocket
{
    internal class TaosWebSocketContext : ITaosContext
    {

        public int AffectRows { get; set; }
        public int FieldCount { get; set; }
        private IntPtr rowdata;
        private long _id;
        private WSFetchRsp _fetch;
        private WSQueryRsp _meta;
        private readonly DateTime _dt1970;

  
          
        public bool CloseConnection { get; set; }

        public TaosWebSocketContext(TaosWSResult tr)
        {
            _dt1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            AffectRows = tr.meta.affected_rows;
            FieldCount = tr.meta.fields_count;
            var bufferid = new byte[8];
            Array.Copy(tr.data, bufferid, 8);
             _id = BitConverter.ToInt64(bufferid,0);
            rowdata = Marshal.AllocHGlobal(tr.block_length-8);
            Marshal.Copy(tr.data, 8, rowdata, tr.block_length-8);
            _fetch = tr.fetch;
            _meta = tr.meta;
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(rowdata);
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
            return _meta.fields_lengths[ordinal];
        }

        private IntPtr GetValuePtr(int ordinal)
        {
            int offset = IntPtr.Size * ordinal;
            return Marshal.ReadIntPtr(rowdata, offset);
        }

    
        public Type GetFieldType(int ordinal)
        {
            var t= (TDengineDataType)_meta.fields_types[ordinal];
            return t.ToCrlType();
        }

        public string GetName(int ordinal)
        {
            return _meta.fields_names[ordinal];
        }

        public int GetOrdinal(string name)
        {
            return _meta.fields_names.IndexOf(name);
        }

        public Stream GetStream(int ordinal)
        {
            throw new NotImplementedException();
        }

        public TimeSpan GetTimeSpan(int ordinal)
        {
            throw new NotImplementedException();
        }
        int _index = 0;
        public object GetValue(int ordinal)
        {
            object result = null;
            if (ordinal >= 0 && ordinal < FieldCount)
            {
                var meta = new { type = _meta.fields_types[ordinal] };
                int offset = IntPtr.Size * ordinal;
                IntPtr data = Marshal.ReadIntPtr(rowdata, offset);
                if (data != IntPtr.Zero)
                {
                    switch ((TDengineDataType)meta.type)
                    {
                        case TDengineDataType.TSDB_DATA_TYPE_BOOL:
                            bool v1 = Marshal.ReadByte(data) == 0 ? false : true;
                            result = v1;
                            break;

                        case TDengineDataType.TSDB_DATA_TYPE_TINYINT:
                            sbyte v2s = (sbyte)Marshal.ReadByte(data);
                            result = v2s;
                            break;

                        case TDengineDataType.TSDB_DATA_TYPE_UTINYINT:
                            byte v2 = Marshal.ReadByte(data);
                            result = v2;
                            break;

                        case TDengineDataType.TSDB_DATA_TYPE_SMALLINT:
                            short v3 = Marshal.ReadInt16(data);
                            result = v3;
                            break;

                        case TDengineDataType.TSDB_DATA_TYPE_USMALLINT:
                            ushort v12 = (ushort)Marshal.ReadInt16(data);
                            result = v12;
                            break;

                        case TDengineDataType.TSDB_DATA_TYPE_INT:
                            int v4 = Marshal.ReadInt32(data);
                            result = v4;
                            break;

                        case TDengineDataType.TSDB_DATA_TYPE_UINT:
                            uint v13 = (uint)Marshal.ReadInt32(data);
                            result = v13;
                            break;

                        case TDengineDataType.TSDB_DATA_TYPE_BIGINT:
                            long v5 = Marshal.ReadInt64(data);
                            result = v5;
                            break;

                        case TDengineDataType.TSDB_DATA_TYPE_UBIGINT:
                            ulong v14 = (ulong)Marshal.ReadInt64(data);
                            result = v14;
                            break;

                        case TDengineDataType.TSDB_DATA_TYPE_FLOAT:
                            float v6 = (float)Marshal.PtrToStructure(data, typeof(float));
                            result = v6;
                            break;

                        case TDengineDataType.TSDB_DATA_TYPE_DOUBLE:
                            double v7 = (double)Marshal.PtrToStructure(data, typeof(double));
                            result = v7;
                            break;

                        case TDengineDataType.TSDB_DATA_TYPE_VARCHAR:
                            {
#if NET5_0_OR_GREATER
                                string v8 = Marshal.PtrToStringUTF8(data, GetFieldSize(ordinal));
                                result = v8?.RemoveNull();
#else
                                byte[] buffer = new byte[GetFieldSize(ordinal)];
                                Marshal.Copy(data, buffer, 0, buffer.Length);
                                string v8 = Encoding.UTF8.GetString(buffer);
                                result = v8?.RemoveNull();
#endif
                            }
                            break;

                        case TDengineDataType.TSDB_DATA_TYPE_TIMESTAMP:
                            {
                                result = GetDateTimeFrom(data, (TSDB_TIME_PRECISION)_meta.precision);
                            }
                            break;

                        case TDengineDataType.TSDB_DATA_TYPE_JSONTAG:
                        case TDengineDataType.TSDB_DATA_TYPE_NCHAR:
                            {
                                string v10 = string.Empty;
                                int contentLength = GetFieldSize(ordinal);
                                if (contentLength > 0)// https://github.com/maikebing/Maikebing.EntityFrameworkCore.Taos/issues/99
                                {
                                    byte[] bf = new byte[contentLength];
                                    Marshal.Copy(data, bf, 0, contentLength);

                                    if (bf.IsUTF8Bytes() || (bf[0] == 0xEF && bf[1] == 0xBB && bf[2] == 0xBF))
                                    {
                                        v10 = System.Text.Encoding.UTF8.GetString(bf)?.RemoveNull();
                                    }
                                    else
                                    {
                                        v10 = System.Text.Encoding.GetEncoding(936).GetString(bf)?.RemoveNull();
                                    }
                                }
                                result = v10;
                            }
                            break;

                        case TDengineDataType.TSDB_DATA_TYPE_NULL:
                            result = null;
                            break;
                    }
                }
                else
                {
                    result = null;
                }
            }
            return result;
        }
        private DateTime GetDateTimeFrom(IntPtr data, TSDB_TIME_PRECISION _dateTimePrecision)
        {
            var val = Marshal.ReadInt64(data);
            //double tsp;
            switch (_dateTimePrecision)
            {
                /*
                * ticks为100纳秒，必须乘以10才能达到微秒级的区分度
                * 1秒s    = 1000毫秒ms
                * 1毫秒ms = 1000微秒us
                * 1微秒us = 1000纳秒ns
                * 因此， 1毫秒ms = 1000000纳秒ns = 10000ticks
                */
                case TSDB_TIME_PRECISION.TSDB_TIME_PRECISION_NANO:
                    val /= 100;
                    break;

                case TSDB_TIME_PRECISION.TSDB_TIME_PRECISION_MICRO:
                    val *= 10;
                    break;

                case TSDB_TIME_PRECISION.TSDB_TIME_PRECISION_MILLI:
                default:
                    val *= 10000;
                    break;
            }
            var v9 = _dt1970.AddTicks(val);
            return v9.ToLocalTime();
        }
        public TaosException LastException()
        {
            throw new NotImplementedException();
        }

        public bool Read()
        {
            _index++;
            return _index <= _fetch.rows;
        }
    }
}