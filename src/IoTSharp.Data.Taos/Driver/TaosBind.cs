using System;
using System.Runtime.InteropServices;

namespace TDengineDriver
{
    /// <summary>
    /// this class used to get an instance of struct of TAO_BIND or TAOS_MULTI_BIND
    /// And the instance is corresponding with TDengine data type. For example, calling 
    /// "bindBinary"  will return a TAOS_BIND object that is corresponding with TDengine's
    /// binary type.
    /// </summary>
    public class TaosBind
    {
        public static TAOS_BIND BindBool(bool val)
        {
            TAOS_BIND bind = new TAOS_BIND();
            byte[] boolByteArr = BitConverter.GetBytes(val);
            int boolByteArrSize = Marshal.SizeOf(boolByteArr[0]) * boolByteArr.Length;
            IntPtr c_bool = Marshal.AllocHGlobal(1);
            Marshal.Copy(boolByteArr, 0, c_bool, boolByteArr.Length);

            int length = sizeof(Boolean);
            IntPtr lengthPtr = Marshal.AllocHGlobal(sizeof(int));
            Marshal.WriteInt32(lengthPtr, length);

            bind.buffer_type = (int)TDengineDataType.TSDB_DATA_TYPE_BOOL;
            bind.buffer = c_bool;
            bind.buffer_length = length;
            bind.length = lengthPtr;
            bind.is_null = IntPtr.Zero;

            return bind;
        }
        public static TAOS_BIND BindTinyInt(sbyte val)
        {
            TAOS_BIND bind = new TAOS_BIND();

            byte[] tinyIntByteArr = BitConverter.GetBytes(val);
            int tinyIntByteArrSize = Marshal.SizeOf(tinyIntByteArr[0]) * tinyIntByteArr.Length;
            IntPtr c_tinyint = Marshal.AllocHGlobal(tinyIntByteArrSize);
            Marshal.Copy(tinyIntByteArr, 0, c_tinyint, tinyIntByteArr.Length);

            int length = sizeof(sbyte);
            IntPtr lengthPtr = Marshal.AllocHGlobal(sizeof(int));
            Marshal.WriteInt32(lengthPtr, length);

            bind.buffer_type = (int)TDengineDataType.TSDB_DATA_TYPE_TINYINT;
            bind.buffer = c_tinyint;
            bind.buffer_length = length;
            bind.length = lengthPtr;
            bind.is_null = IntPtr.Zero;
            return bind;

        }

        public static TAOS_BIND BindSmallInt(short val)
        {

            TAOS_BIND bind = new TAOS_BIND();
            IntPtr c_smallint = Marshal.AllocHGlobal(sizeof(short));
            Marshal.WriteInt16(c_smallint, val);

            int length = sizeof(short);
            IntPtr lengthPtr = Marshal.AllocHGlobal(sizeof(int));
            Marshal.WriteInt32(lengthPtr, length);

            bind.buffer_type = (int)TDengineDataType.TSDB_DATA_TYPE_SMALLINT;
            bind.buffer = c_smallint;
            bind.buffer_length = length;
            bind.length = lengthPtr;
            bind.is_null = IntPtr.Zero;

            return bind;
        }

        public static TAOS_BIND BindInt(int val)
        {
            TAOS_BIND bind = new TAOS_BIND();
            IntPtr c_int = Marshal.AllocHGlobal(sizeof(int));
            Marshal.WriteInt32(c_int, val);

            int length = sizeof(int);
            IntPtr lengthPtr = Marshal.AllocHGlobal(sizeof(int));
            Marshal.WriteInt32(lengthPtr, length);

            bind.buffer_type = (int)TDengineDataType.TSDB_DATA_TYPE_INT;
            bind.buffer = c_int;
            bind.buffer_length = length;
            bind.length = lengthPtr;
            bind.is_null = IntPtr.Zero;

            return bind;
        }

        public static TAOS_BIND BindBigInt(long val)
        {

            TAOS_BIND bind = new TAOS_BIND();
            IntPtr c_bigint = Marshal.AllocHGlobal(sizeof(long));
            Marshal.WriteInt64(c_bigint, val);

            int length = sizeof(long);
            IntPtr lengthPtr = Marshal.AllocHGlobal(sizeof(int));
            Marshal.WriteInt32(lengthPtr, length);

            bind.buffer_type = (int)TDengineDataType.TSDB_DATA_TYPE_BIGINT;
            bind.buffer = c_bigint;
            bind.buffer_length = length;
            bind.length = lengthPtr;
            bind.is_null = IntPtr.Zero;

            return bind;
        }

        public static TAOS_BIND BindUTinyInt(byte val)
        {
            TAOS_BIND bind = new TAOS_BIND();

            IntPtr c_unsigned_tinyint = Marshal.AllocHGlobal(sizeof(byte));
            Marshal.WriteByte(c_unsigned_tinyint, val);

            int length = sizeof(byte);
            IntPtr lengthPtr = Marshal.AllocHGlobal(sizeof(int));
            Marshal.WriteInt32(lengthPtr, length);

            bind.buffer_type = (int)TDengineDataType.TSDB_DATA_TYPE_UTINYINT;
            bind.buffer = c_unsigned_tinyint;
            bind.buffer_length = length;
            bind.length = lengthPtr;
            bind.is_null = IntPtr.Zero;

            return bind;
        }

        public static TAOS_BIND BindUSmallInt(UInt16 val)
        {
            TAOS_BIND bind = new TAOS_BIND();

            byte[] uSmallIntByteArr = BitConverter.GetBytes(val);
            int usmallSize = Marshal.SizeOf(uSmallIntByteArr[0]) * uSmallIntByteArr.Length;
            IntPtr c_unsigned_smallint = Marshal.AllocHGlobal(usmallSize);
            Marshal.Copy(uSmallIntByteArr, 0, c_unsigned_smallint, uSmallIntByteArr.Length);

            int length = sizeof(UInt16);
            IntPtr lengthPtr = Marshal.AllocHGlobal(sizeof(int));
            Marshal.WriteInt32(lengthPtr, length);

            bind.buffer_type = (int)TDengineDataType.TSDB_DATA_TYPE_USMALLINT;
            bind.buffer = c_unsigned_smallint;
            bind.buffer_length = length;
            bind.length = lengthPtr;
            bind.is_null = IntPtr.Zero;

            return bind;
        }

        public static TAOS_BIND BindUInt(uint val)
        {
            TAOS_BIND bind = new TAOS_BIND();

            byte[] byteArr = BitConverter.GetBytes(val);
            int usmallSize = Marshal.SizeOf(byteArr[0]) * byteArr.Length;
            IntPtr c_unsigned_int = Marshal.AllocHGlobal(usmallSize);
            Marshal.Copy(byteArr, 0, c_unsigned_int, byteArr.Length);

            int length = sizeof(uint);
            IntPtr lengthPtr = Marshal.AllocHGlobal(sizeof(int));
            Marshal.WriteInt32(lengthPtr, length);

            bind.buffer_type = (int)TDengineDataType.TSDB_DATA_TYPE_UINT;
            bind.buffer = c_unsigned_int;
            bind.buffer_length = length;
            bind.length = lengthPtr;
            bind.is_null = IntPtr.Zero;

            return bind;
        }

        public static TAOS_BIND BindUBigInt(ulong val)
        {
            TAOS_BIND bind = new TAOS_BIND();

            byte[] byteArr = BitConverter.GetBytes(val);
            int usmallSize = Marshal.SizeOf(byteArr[0]) * byteArr.Length;
            IntPtr c_unsigned_bigint = Marshal.AllocHGlobal(usmallSize);
            Marshal.Copy(byteArr, 0, c_unsigned_bigint, byteArr.Length);

            int length = sizeof(ulong);
            IntPtr lengthPtr = Marshal.AllocHGlobal(sizeof(int));
            Marshal.WriteInt32(lengthPtr, length);

            bind.buffer_type = (int)TDengineDataType.TSDB_DATA_TYPE_UBIGINT;
            bind.buffer = c_unsigned_bigint;
            bind.buffer_length = length;
            bind.length = lengthPtr;
            bind.is_null = IntPtr.Zero;

            return bind;
        }

        public static TAOS_BIND BindFloat(float val)
        {
            TAOS_BIND bind = new TAOS_BIND();

            byte[] byteArr = BitConverter.GetBytes(val);
            int floatByteArrSize = Marshal.SizeOf(byteArr[0]) * byteArr.Length;
            IntPtr c_float = Marshal.AllocHGlobal(floatByteArrSize);
            Marshal.Copy(byteArr, 0, c_float, byteArr.Length);

            int length = sizeof(float);
            IntPtr lengthPtr = Marshal.AllocHGlobal(sizeof(int));
            Marshal.WriteInt32(lengthPtr, length);

            bind.buffer_type = (int)TDengineDataType.TSDB_DATA_TYPE_FLOAT;
            bind.buffer = c_float;
            bind.buffer_length = length;
            bind.length = lengthPtr;
            bind.is_null = IntPtr.Zero;

            return bind;
        }

        public static TAOS_BIND BindDouble(Double val)
        {
            TAOS_BIND bind = new TAOS_BIND();

            byte[] byteArr = BitConverter.GetBytes(val);
            int doubleByteArrSize = Marshal.SizeOf(byteArr[0]) * byteArr.Length;
            IntPtr c_double = Marshal.AllocHGlobal(doubleByteArrSize);
            Marshal.Copy(byteArr, 0, c_double, byteArr.Length);

            int length = sizeof(Double);
            IntPtr lengthPtr = Marshal.AllocHGlobal(sizeof(int));
            Marshal.WriteInt32(lengthPtr, length);

            bind.buffer_type = (int)TDengineDataType.TSDB_DATA_TYPE_DOUBLE;
            bind.buffer = c_double;
            bind.buffer_length = length;
            bind.length = lengthPtr;
            bind.is_null = IntPtr.Zero;

            return bind;
        }

        public static TAOS_BIND BindBinary(String val)
        {

            TAOS_BIND bind = new TAOS_BIND();
            UTF8PtrStruct utf8PtrStruct = new UTF8PtrStruct(val);
            IntPtr lenPtr = Marshal.AllocHGlobal(sizeof(ulong));
            Marshal.WriteInt64(lenPtr, utf8PtrStruct.utf8StrLength);

            bind.buffer_type = (int)TDengineDataType.TSDB_DATA_TYPE_BINARY;
            bind.buffer = utf8PtrStruct.utf8Ptr;
            bind.buffer_length = utf8PtrStruct.utf8StrLength;
            bind.length = lenPtr;
            bind.is_null = IntPtr.Zero;

            return bind;
        }
        public static TAOS_BIND BindNchar(String val)
        {
            TAOS_BIND bind = new TAOS_BIND();
            UTF8PtrStruct utf8PtrStruct = new UTF8PtrStruct(val);
            IntPtr lenPtr = Marshal.AllocHGlobal(sizeof(ulong));
            Marshal.WriteInt64(lenPtr, utf8PtrStruct.utf8StrLength);

            bind.buffer_type = (int)TDengineDataType.TSDB_DATA_TYPE_NCHAR;
            bind.buffer = utf8PtrStruct.utf8Ptr;
            bind.buffer_length = utf8PtrStruct.utf8StrLength;
            bind.length = lenPtr;
            bind.is_null = IntPtr.Zero;

            return bind;
        }

        public static TAOS_BIND BindJSON(String val)
        {
            TAOS_BIND bind = new TAOS_BIND();
            UTF8PtrStruct utf8PtrStruct = new UTF8PtrStruct(val);
            IntPtr lenPtr = Marshal.AllocHGlobal(sizeof(ulong));
            Marshal.WriteInt64(lenPtr, utf8PtrStruct.utf8StrLength);

            bind.buffer_type = (int)TDengineDataType.TSDB_DATA_TYPE_JSONTAG;
            bind.buffer = utf8PtrStruct.utf8Ptr;
            bind.buffer_length = utf8PtrStruct.utf8StrLength;
            bind.length = lenPtr;
            bind.is_null = IntPtr.Zero;

            return bind;
        }

        public static TAOS_BIND BindNil()
        {
            TAOS_BIND bind = new TAOS_BIND();

            int isNull = 1;//IntPtr.Size;
            IntPtr lenPtr = Marshal.AllocHGlobal(sizeof(int));
            Marshal.WriteInt32(lenPtr, isNull);

            bind.buffer_type = (int)TDengineDataType.TSDB_DATA_TYPE_NULL;
            bind.is_null = lenPtr;
            return bind;
        }

        public static TAOS_BIND BindTimestamp(long ts)
        {

            TAOS_BIND bind = new TAOS_BIND();
            IntPtr c_long = Marshal.AllocHGlobal(sizeof(long));
            Marshal.WriteInt64(c_long, ts);

            int length = sizeof(long);
            IntPtr lengthPtr = Marshal.AllocHGlobal(4);
            Marshal.WriteInt32(lengthPtr, length);

            bind.buffer_type = (int)TDengineDataType.TSDB_DATA_TYPE_TIMESTAMP;
            bind.buffer = c_long;
            bind.buffer_length = length;
            bind.length = lengthPtr;
            bind.is_null = IntPtr.Zero;

            return bind;

        }


        public static void FreeTaosBind(TAOS_BIND[] binds)
        {
            foreach (TAOS_BIND bind in binds)
            {
                Marshal.FreeHGlobal(bind.length);
                if (bind.buffer_type == (int)TDengineDataType.TSDB_DATA_TYPE_BINARY || bind.buffer_type == (int)TDengineDataType.TSDB_DATA_TYPE_NCHAR || bind.buffer_type == (int)TDengineDataType.TSDB_DATA_TYPE_JSONTAG)
                {
#if NETSTANDARD2_1_OR_GREATER ||NET5_0_OR_GREATER||NETCOREAPP1_1_OR_GREATER
                    Marshal.FreeCoTaskMem(bind.buffer);
#else
                    Marshal.FreeHGlobal(bind.buffer);
#endif
                }
                else
                {
                    Marshal.FreeHGlobal(bind.buffer);
                }

                if (bind.is_null != IntPtr.Zero)
                {

                    Marshal.FreeHGlobal(bind.is_null);
                }

            }
        }
        public static TAOS_BIND[] GetNTableCNRow()
        {
            TAOS_BIND[] binds = new TAOS_BIND[15];
            binds[0] = TaosBind.BindTimestamp(1637064040000);
            binds[1] = TaosBind.BindTinyInt(-2);
            binds[2] = TaosBind.BindSmallInt(short.MaxValue);
            binds[3] = TaosBind.BindInt(int.MaxValue);
            binds[4] = TaosBind.BindBigInt(Int64.MaxValue);
            binds[5] = TaosBind.BindUTinyInt(byte.MaxValue - 1);
            binds[6] = TaosBind.BindUSmallInt(UInt16.MaxValue - 1);
            binds[7] = TaosBind.BindUInt(uint.MinValue + 1);
            binds[8] = TaosBind.BindUBigInt(UInt64.MinValue + 1);
            binds[9] = TaosBind.BindFloat(11.11F);
            binds[10] = TaosBind.BindDouble(22.22D);
            binds[11] = TaosBind.BindBinary("TDengine数据");
            binds[12] = TaosBind.BindNchar("taosdata涛思数据");
            binds[13] = TaosBind.BindBool(true);
            binds[14] = TaosBind.BindNil();
            return binds;
        }
    }

}