/*
 * Copyright (c) 2019 TAOS Data, Inc. <jhtao@taosdata.com>
 *
 * This program is free software: you can use, redistribute, and/or modify
 * it under the terms of the GNU Affero General Public License, version 3
 * or later ("AGPL"), as published by the Free Software Foundation.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace TDengineDriver
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct taosField
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 65)]
        public byte[] name;

        [MarshalAs(UnmanagedType.U1, SizeConst = 1)]
        public byte type;

        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public ushort bytes;
    }

    internal enum TDengineDataType
    {
        TSDB_DATA_TYPE_BOOL = 1,
        TSDB_DATA_TYPE_TINYINT = 2,
        TSDB_DATA_TYPE_SMALLINT = 3,
        TSDB_DATA_TYPE_INT = 4,
        TSDB_DATA_TYPE_BIGINT = 5,
        TSDB_DATA_TYPE_FLOAT = 6,
        TSDB_DATA_TYPE_DOUBLE = 7,
        TSDB_DATA_TYPE_BINARY = 8,
        TSDB_DATA_TYPE_TIMESTAMP = 9,
        TSDB_DATA_TYPE_NCHAR = 10
    }
    public enum TSDB_TIME_PRECISION:int
    {
        TSDB_TIME_PRECISION_MILLI = 0,
        TSDB_TIME_PRECISION_MICRO = 1,
        TSDB_TIME_PRECISION_NANO = 2
    }
    internal enum TDengineInitOption
    {
        TSDB_OPTION_LOCALE = 0,
        TSDB_OPTION_CHARSET = 1,
        TSDB_OPTION_TIMEZONE = 2,
        TDDB_OPTION_CONFIGDIR = 3,
        TDDB_OPTION_SHELL_ACTIVITY_TIMER = 4
    }

    internal class TDengineMeta
    {
        public string name;
        public ushort size;
        public byte type;

        public string TypeName()
        {
            switch ((TDengineDataType)type)
            {
                case TDengineDataType.TSDB_DATA_TYPE_BOOL:
                    return "BOOLEAN";

                case TDengineDataType.TSDB_DATA_TYPE_TINYINT:
                    return "BYTE";

                case TDengineDataType.TSDB_DATA_TYPE_SMALLINT:
                    return "SHORT";

                case TDengineDataType.TSDB_DATA_TYPE_INT:
                    return "INT";

                case TDengineDataType.TSDB_DATA_TYPE_BIGINT:
                    return "LONG";

                case TDengineDataType.TSDB_DATA_TYPE_FLOAT:
                    return "FLOAT";

                case TDengineDataType.TSDB_DATA_TYPE_DOUBLE:
                    return "DOUBLE";

                case TDengineDataType.TSDB_DATA_TYPE_BINARY:
                    return "STRING";

                case TDengineDataType.TSDB_DATA_TYPE_TIMESTAMP:
                    return "TIMESTAMP";

                case TDengineDataType.TSDB_DATA_TYPE_NCHAR:
                    return "NCHAR";

                default:
                    return "undefine";
            }
        }
    }

    internal class TDengine
    {
        public const int TSDB_CODE_SUCCESS = 0;

        [DllImport("taos", EntryPoint = "taos_init", CallingConvention = CallingConvention.StdCall)]
        public static extern void Init();

        [DllImport("taos", EntryPoint = "taos_options", CallingConvention = CallingConvention.StdCall)]
        public static extern void Options(int option, string value);

        [DllImport("taos", EntryPoint = "taos_connect", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr Connect(string ip, string user, string password, string db, int port);

        [DllImport("taos", EntryPoint = "taos_errstr", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr taos_errstr(IntPtr taos);

        public static string Error(IntPtr conn)
        {
            IntPtr errPtr = taos_errstr(conn);
            return Marshal.PtrToStringAnsi(errPtr);
        }

        [DllImport("taos", EntryPoint = "taos_errno", CallingConvention = CallingConvention.StdCall)]
        public static extern int ErrorNo(IntPtr taos);

        [DllImport("taos", EntryPoint = "taos_query", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr Query(IntPtr taos, string sqlstr);

        [DllImport("taos", EntryPoint = "taos_affected_rows", CallingConvention = CallingConvention.StdCall)]
        public static extern int AffectRows(IntPtr res);

        [DllImport("taos", EntryPoint = "taos_field_count", CallingConvention = CallingConvention.StdCall)]
        public static extern int FieldCount(IntPtr res);

        [DllImport("taos", EntryPoint = "taos_fetch_fields", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr taos_fetch_fields(IntPtr res);

        public static List<TDengineMeta> FetchFields(IntPtr res)
        {
            const int fieldSize = 68;

            List<TDengineMeta> metas = new List<TDengineMeta>();

            int fieldCount = FieldCount(res);
            IntPtr fieldsPtr = taos_fetch_fields(res);

            for (int i = 0; i < fieldCount; ++i)
            {
                int offset = i * fieldSize;
                taosField field = Marshal.PtrToStructure<taosField>(fieldsPtr + offset);
                TDengineMeta meta = new TDengineMeta() { name = Encoding.Default.GetString(field.name)?.TrimEnd('\0'), size = field.bytes, type = field.type };
                metas.Add(meta);
            }

            return metas;
        }

        [DllImport("taos", EntryPoint = "taos_fetch_row", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr FetchRows(IntPtr res);

        [DllImport("taos", EntryPoint = "taos_free_result", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr FreeResult(IntPtr res);

        [DllImport("taos", EntryPoint = "taos_close", CallingConvention = CallingConvention.StdCall)]
        public static extern int Close(IntPtr taos);

        [DllImport("taos", EntryPoint = "taos_cleanup", CallingConvention = CallingConvention.StdCall)]
        public static extern void Cleanup();

        [DllImport("taos", EntryPoint = "taos_get_client_info", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr GetClientInfo();

        [DllImport("taos", EntryPoint = "taos_get_server_info", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr GetServerInfo(IntPtr taos);

        [DllImport("taos", EntryPoint = "taos_select_db", CallingConvention = CallingConvention.StdCall)]
        public static extern int SelectDatabase(IntPtr taos, string db);

        [DllImport("taos", EntryPoint = "taos_result_precision", CallingConvention = CallingConvention.StdCall)]
        public static extern int ResultPrecision(IntPtr rest);
        
    }
}