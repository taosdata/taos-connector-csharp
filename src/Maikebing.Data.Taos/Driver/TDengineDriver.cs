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

namespace TDengineDriver
{
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
        public short size;
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
        public const int TSDB_CODE_DB_ALREADY_EXIST = 33;
        public const int TSDB_CODE_TABLE_ALREADY_EXIST = 34;

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
        static extern public int Query(IntPtr taos, string sqlstr);

        [DllImport("taos", EntryPoint = "taos_stmt_init", CallingConvention = CallingConvention.StdCall)]
        static extern public IntPtr taos_stmt_init(IntPtr taos);

        [DllImport("taos", EntryPoint = "taos_stmt_init", CallingConvention = CallingConvention.StdCall)]
        static extern public int taos_stmt_prepare(IntPtr stmt, string sql, Int64 length);


        [DllImport("taos", EntryPoint = "taos_affected_rows", CallingConvention = CallingConvention.StdCall)]
        static extern public int AffectRows(IntPtr taos);

        [DllImport("taos", EntryPoint = "taos_use_result", CallingConvention = CallingConvention.StdCall)]
        static extern public IntPtr UseResult(IntPtr taos);

        [DllImport("taos", EntryPoint = "taos_field_count", CallingConvention = CallingConvention.StdCall)]
        static extern public int FieldCount(IntPtr taos);

        [DllImport("taos", EntryPoint = "taos_fetch_fields", CallingConvention = CallingConvention.StdCall)]

        static extern private IntPtr taos_fetch_fields(IntPtr res);
        static public List<TDengineMeta> FetchFields(IntPtr taos)
        {
            const int fieldSize = 68;

            List<TDengineMeta> metas = new List<TDengineMeta>();
            IntPtr result = TDengine.UseResult(taos);
            if (result == IntPtr.Zero)
            {
                return metas;
            }

            int fieldCount = FieldCount(taos);
            IntPtr fieldsPtr = taos_fetch_fields(result);

            for (int i = 0; i < fieldCount; ++i)
            {
                int offset = i * fieldSize;

                TDengineMeta meta = new TDengineMeta();
                meta.name = Marshal.PtrToStringAnsi(fieldsPtr + offset);
                meta.size = Marshal.ReadInt16(fieldsPtr + offset + 64);
                meta.type = Marshal.ReadByte(fieldsPtr + offset + 66);
                metas.Add(meta);
            }

            return metas;
        }

        [DllImport("taos", EntryPoint = "taos_fetch_row", CallingConvention = CallingConvention.Cdecl)]
        static extern public IntPtr FetchRows(IntPtr res);

        [DllImport("taos", EntryPoint = "taos_free_result", CallingConvention = CallingConvention.Cdecl)]
        static extern public IntPtr FreeResult(IntPtr res);

        [DllImport("taos", EntryPoint = "taos_close", CallingConvention = CallingConvention.Cdecl)]
        static extern public int Close(IntPtr taos);
    }
}