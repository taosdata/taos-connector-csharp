using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using TDengineDriver;

namespace IoTSharp.Data.Taos.Protocols
{
    internal class TaosNativeContext : ITaosContext
    {
        private taosField[] metas;
        private IntPtr taos;
        private IntPtr ptr;
        private IntPtr rowdata;
        private double _date_max_1970;
        private DateTime _dt1970;
        public int AffectRows { get; set; }
        public int FieldCount { get; set; }

        private IntPtr _taosResult;
        private readonly List<taosField> _metas;

        public bool CloseConnection { get; set; }

        public TaosNativeContext(taosField[] metas, bool closeConnection, nint taos, IntPtr ptr, int affectRows, int fieldcount)
        {
            this.metas = metas;
            this.CloseConnection = closeConnection;
            this.taos = taos;
            this.ptr = ptr;
            this.AffectRows = affectRows;
            this.FieldCount = fieldcount;
            _taosResult = ptr;
            _metas = metas == null ? new List<taosField>() : new List<taosField>(metas);
            _dt1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            _date_max_1970 = DateTime.MaxValue.Subtract(_dt1970).TotalMilliseconds;
        }

        public bool Read()
        {
            rowdata = TDengine.FetchRows(_taosResult);
            return rowdata != IntPtr.Zero;
        }

        public TaosException LastException()
        {
            return TDengine.ErrorNo(_taosResult) == 0 ? null : new TaosException(new TaosErrorResult() { Code = TDengine.ErrorNo(_taosResult), Error = TDengine.Error(_taosResult) }, null);
        }

        public int GetErrorNo()
        {
            return TDengine.ErrorNo(_taosResult);
        }

        public void Dispose()
        {
            if (_taosResult != IntPtr.Zero)
            {
                TDengine.FreeResult(_taosResult);
                _taosResult = IntPtr.Zero;
            }
            if (rowdata != IntPtr.Zero)
            {
                TDengine.FreeResult(rowdata);
                rowdata = IntPtr.Zero;
            }
        }

        private IntPtr GetValuePtr(int ordinal)
        {
            int offset = IntPtr.Size * ordinal;
            return Marshal.ReadIntPtr(rowdata, offset);
        }

        public int GetFieldSize(int ordinal)
        {
            IntPtr colLengthPrt = TDengine.FetchLengths(_taosResult);
            int numOfFiled = TDengine.FieldCount(_taosResult);
            int[] colLengthArr = new int[numOfFiled];
            Marshal.Copy(colLengthPrt, colLengthArr, 0, numOfFiled);
            return colLengthArr[ordinal];
        }

        public static System.Text.Encoding GetType(FileStream fs)
        {
            byte[] Unicode = new byte[] { 0xFF, 0xFE, 0x41 };
            byte[] UnicodeBIG = new byte[] { 0xFE, 0xFF, 0x00 };
            byte[] UTF8 = new byte[] { 0xEF, 0xBB, 0xBF }; //带BOM
            Encoding reVal = Encoding.Default;

            BinaryReader r = new BinaryReader(fs, System.Text.Encoding.Default);
            int i;
            int.TryParse(fs.Length.ToString(), out i);
            byte[] ss = r.ReadBytes(i);
            if (ss.IsUTF8Bytes() || (ss[0] == 0xEF && ss[1] == 0xBB && ss[2] == 0xBF))
            {
                reVal = Encoding.UTF8;
            }
            else if (ss[0] == 0xFE && ss[1] == 0xFF && ss[2] == 0x00)
            {
                reVal = Encoding.BigEndianUnicode;
            }
            else if (ss[0] == 0xFF && ss[1] == 0xFE && ss[2] == 0x41)
            {
                reVal = Encoding.Unicode;
            }
            r.Close();
            return reVal;
        }

      
        public object GetValue(int ordinal)
        {
            object result = null;
            if (ordinal >= 0 && ordinal < _metas.Count)
            {
                var meta = _metas[ordinal];
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
                                result = GetDateTimeFrom(data);
                            }
                            break;

                        case TDengineDataType.TSDB_DATA_TYPE_JSON:
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

        public DateTime GetDataTime(int ordinal)
        {
            return GetDateTimeFrom(GetValuePtr(ordinal));
        }

        private DateTime GetDateTimeFrom(IntPtr data)
        {
            var val = Marshal.ReadInt64(data);
            //double tsp;
            var _dateTimePrecision = (TSDB_TIME_PRECISION)TDengine.ResultPrecision(_taosResult);
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

        public Stream GetStream(int ordinal)
        {
            MemoryStream result = null;
            var meta = _metas[ordinal];
            int offset = IntPtr.Size * ordinal;
            IntPtr data = Marshal.ReadIntPtr(rowdata, offset);
            if (data != IntPtr.Zero)
            {
                byte[] bf = new byte[meta.Size];
                Marshal.Copy(data, bf, 0, meta.Size);
                result = new MemoryStream(bf);
            }
            return result;
        }

        public bool GetBoolean(int ordinal) => Marshal.ReadByte(GetValuePtr(ordinal)) == 0 ? false : true;

        public byte GetByte(int ordinal) => Marshal.ReadByte(GetValuePtr(ordinal));

        public long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            byte[] buffer1 = new byte[length + bufferOffset];
            Marshal.Copy(GetValuePtr(ordinal), buffer1, (int)dataOffset, length + bufferOffset);
            Array.Copy(buffer1, bufferOffset, buffer, 0, length);
            return length;
        }

        public TimeSpan GetTimeSpan(int ordinal)
        {
            var val = Marshal.ReadInt64(GetValuePtr(ordinal));
            var _dateTimePrecision = (TSDB_TIME_PRECISION)TDengine.ResultPrecision(_taosResult);
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
                    return TimeSpan.FromTicks(val / 100);

                case TSDB_TIME_PRECISION.TSDB_TIME_PRECISION_MICRO:
                    return TimeSpan.FromTicks(val * 10);

                case TSDB_TIME_PRECISION.TSDB_TIME_PRECISION_MILLI:
                default:
                    return TimeSpan.FromTicks(val * 10000);
            }
        }

      

        public string GetName(int ordinal) => _metas[ordinal].Name;

        public int GetOrdinal(string name)
                    => _metas.IndexOf(_metas.FirstOrDefault(m => m.Name == name));

        public Type GetFieldType(int ordinal)
        {
            if (_metas == null || ordinal >= _metas.Count)
            {
                throw new InvalidOperationException($"DataReaderClosed{nameof(GetFieldType)}");
            }
            return _metas[ordinal].CrlType;
        }

      
    }
}