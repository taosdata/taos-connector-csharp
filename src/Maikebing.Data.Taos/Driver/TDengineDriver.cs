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

using Microsoft.Extensions.Logging;
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

    enum TDengineDataType
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

    enum TDengineInitOption
    {
        TSDB_OPTION_LOCALE = 0,
        TSDB_OPTION_CHARSET = 1,
        TSDB_OPTION_TIMEZONE = 2,
        TDDB_OPTION_CONFIGDIR = 3,
        TDDB_OPTION_SHELL_ACTIVITY_TIMER = 4
    }

    class TDengineMeta
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

    class TDengine
    {
        public const int TSDB_CODE_SUCCESS = 0;

        [DllImport("taos", EntryPoint = "taos_init", CallingConvention = CallingConvention.StdCall)]
        static extern public void Init();

        [DllImport("taos", EntryPoint = "taos_options", CallingConvention = CallingConvention.StdCall)]
        static extern public void Options(int option, string value);

        [DllImport("taos", EntryPoint = "taos_connect", CallingConvention = CallingConvention.StdCall)]
        static extern public IntPtr Connect(string ip, string user, string password, string db, int port);

        [DllImport("taos", EntryPoint = "taos_errstr", CallingConvention = CallingConvention.StdCall)]
        static extern private IntPtr taos_errstr(IntPtr taos);
        static public string Error(IntPtr conn)
        {
            IntPtr errPtr = taos_errstr(conn);
            return Marshal.PtrToStringAnsi(errPtr);
        }

        [DllImport("taos", EntryPoint = "taos_errno", CallingConvention = CallingConvention.StdCall)]
        static extern public int ErrorNo(IntPtr taos);

        [DllImport("taos", EntryPoint = "taos_query", CallingConvention = CallingConvention.StdCall)]
        static extern public IntPtr Query(IntPtr taos, string sqlstr);

        [DllImport("taos", EntryPoint = "taos_affected_rows", CallingConvention = CallingConvention.StdCall)]
        static extern public int AffectRows(IntPtr taos);


        [DllImport("taos", EntryPoint = "taos_field_count", CallingConvention = CallingConvention.StdCall)]
        static extern public int FieldCount(IntPtr res);

        [DllImport("taos", EntryPoint = "taos_fetch_fields", CallingConvention = CallingConvention.StdCall)]
        static extern private IntPtr taos_fetch_fields(IntPtr res);
        static public List<TDengineMeta> FetchFields(IntPtr res)
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
        static extern public IntPtr FetchRows(IntPtr res);

        [DllImport("taos", EntryPoint = "taos_free_result", CallingConvention = CallingConvention.StdCall)]
        static extern public IntPtr FreeResult(IntPtr res);

        [DllImport("taos", EntryPoint = "taos_close", CallingConvention = CallingConvention.StdCall)]
        static extern public int Close(IntPtr taos);


        [DllImport("taos", EntryPoint = "taos_cleanup", CallingConvention = CallingConvention.StdCall)]
        static extern public void Cleanup();

        [DllImport("taos", EntryPoint = "taos_get_client_info", CallingConvention = CallingConvention.StdCall)]
        static extern  public IntPtr GetClientInfo();

        [DllImport("taos", EntryPoint = "taos_get_server_info", CallingConvention = CallingConvention.StdCall)]
        static extern public IntPtr GetServerInfo(IntPtr taos);
    }
}