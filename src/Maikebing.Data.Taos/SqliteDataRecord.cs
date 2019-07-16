// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using TaosPCL;

namespace Maikebing.Data.Taos
{
    internal class TaosDataRecord : TaosValueReader
    {
        private readonly Taos3_stmt _stmt;
        private readonly TaosConnection _connection;
        private readonly byte[][] _blobCache;

        public TaosDataRecord(Taos3_stmt stmt, TaosConnection connection)
        {
            _stmt = stmt;
            _connection = connection;
            _blobCache = new byte[FieldCount][];
        }

        public virtual object this[string name]
            => GetValue(GetOrdinal(name));

        public virtual object this[int ordinal]
            => GetValue(ordinal);

        public override int FieldCount
            => raw.Taos3_column_count(_stmt);

        protected override double GetDoubleCore(int ordinal)
            => raw.Taos3_column_double(_stmt, ordinal);

        protected override long GetInt64Core(int ordinal)
            => raw.Taos3_column_int64(_stmt, ordinal);

        protected override string GetStringCore(int ordinal)
            => raw.Taos3_column_text(_stmt, ordinal);

        protected override byte[] GetBlobCore(int ordinal)
            => raw.Taos3_column_blob(_stmt, ordinal);

        protected override int GetTaosType(int ordinal)
        {
            var type = raw.Taos3_column_type(_stmt, ordinal);
            if (type == raw.Taos_NULL
                && (ordinal < 0 || ordinal >= FieldCount))
            {
                // NB: Message is provided by the framework
                throw new ArgumentOutOfRangeException(nameof(ordinal), ordinal, message: null);
            }

            return type;
        }

        protected override T GetNull<T>(int ordinal)
            => typeof(T) == typeof(DBNull) || typeof(T) == typeof(object)
                ? (T)(object)DBNull.Value
                : throw new InvalidOperationException(GetOnNullErrorMsg(ordinal));

        public virtual string GetName(int ordinal)
        {
            var name = raw.Taos3_column_name(_stmt, ordinal);
            if (name == null
                && (ordinal < 0 || ordinal >= FieldCount))
            {
                // NB: Message is provided by the framework
                throw new ArgumentOutOfRangeException(nameof(ordinal), ordinal, message: null);
            }

            return name;
        }

        public virtual int GetOrdinal(string name)
        {
            for (var i = 0; i < FieldCount; i++)
            {
                if (GetName(i) == name)
                {
                    return i;
                }
            }

            // NB: Message is provided by framework
            throw new ArgumentOutOfRangeException(nameof(name), name, message: null);
        }

        public virtual string GetDataTypeName(int ordinal)
        {
            var typeName = raw.Taos3_column_decltype(_stmt, ordinal);
            if (typeName != null)
            {
                var i = typeName.IndexOf('(');

                return i == -1
                    ? typeName
                    : typeName.Substring(0, i);
            }

            var TaosType = GetTaosType(ordinal);
            switch (TaosType)
            {
                case raw.Taos_INTEGER:
                    return "INTEGER";

                case raw.Taos_FLOAT:
                    return "REAL";

                case raw.Taos_TEXT:
                    return "TEXT";

                case raw.Taos_BLOB:
                    return "BLOB";

                case raw.Taos_NULL:
                    return "INTEGER";

                default:
                    Debug.Assert(false, "Unexpected column type: " + TaosType);
                    return "INTEGER";
            }
        }

        public virtual Type GetFieldType(int ordinal)
        {
            var TaosType = GetTaosType(ordinal);
            switch (TaosType)
            {
                case raw.Taos_INTEGER:
                    return typeof(long);

                case raw.Taos_FLOAT:
                    return typeof(double);

                case raw.Taos_TEXT:
                    return typeof(string);

                case raw.Taos_BLOB:
                    return typeof(byte[]);

                case raw.Taos_NULL:
                    return typeof(int);

                default:
                    Debug.Assert(false, "Unexpected column type: " + TaosType);
                    return typeof(int);
            }
        }

        public static Type GetFieldType(string type)
        {
            switch (type)
            {
                case "integer":
                    return typeof(long);

                case "real":
                    return typeof(double);

                case "text":
                    return typeof(string);

                case "blob":
                    return typeof(byte[]);

                case null:
                    return typeof(int);

                default:
                    Debug.Assert(false, "Unexpected column type: " + type);
                    return typeof(int);
            }
        }

        public virtual long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            var blob = GetCachedBlob(ordinal);

            long bytesToRead = (long)blob.Length - dataOffset;
            if (buffer != null)
            {
                bytesToRead = System.Math.Min(bytesToRead, length);
                Array.Copy(blob, dataOffset, buffer, bufferOffset, bytesToRead);
            }
            return bytesToRead;
        }

        public virtual long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            var text = GetString(ordinal);

            int charsToRead = text.Length - (int)dataOffset;
            charsToRead = System.Math.Min(charsToRead, length);
            text.CopyTo((int)dataOffset, buffer, bufferOffset, charsToRead);
            return charsToRead;
        }

        public virtual Stream GetStream(int ordinal)
        {
            if (ordinal < 0 || ordinal >= FieldCount)
            {
                throw new ArgumentOutOfRangeException(nameof(ordinal), ordinal, message: null);
            }

            var blobDatabaseName = raw.Taos3_column_database_name(_stmt, ordinal);
            var blobTableName = raw.Taos3_column_table_name(_stmt, ordinal);

            var rowidOrdinal = -1;
            for (var i = 0; i < FieldCount; i++)
            {
                if (i == ordinal)
                {
                    continue;
                }

                var databaseName = raw.Taos3_column_database_name(_stmt, i);
                if (databaseName != blobDatabaseName)
                {
                    continue;
                }

                var tableName = raw.Taos3_column_table_name(_stmt, i);
                if (tableName != blobTableName)
                {
                    continue;
                }

                var columnName = raw.Taos3_column_origin_name(_stmt, i);
                if ((columnName == "rowid") || (columnName == "_rowid_") || (columnName == "oid"))
                {
                    rowidOrdinal = i;
                    break;
                }

                var rc = raw.Taos3_table_column_metadata(
                    _connection.Handle,
                    databaseName,
                    tableName,
                    columnName,
                    out var dataType,
                    out var collSeq,
                    out var notNull,
                    out var primaryKey,
                    out var autoInc);
                TaosException.ThrowExceptionForRC(rc, _connection.Handle);
                if ((dataType == "INTEGER") && (primaryKey != 0))
                {
                    rowidOrdinal = i;
                    break;
                }
            }

            if (rowidOrdinal < 0)
            {
                return new MemoryStream(GetCachedBlob(ordinal), false);
            }

            var blobColumnName = raw.Taos3_column_origin_name(_stmt, ordinal);
            var rowid = GetInt32(rowidOrdinal);

            return new TaosBlob(_connection, blobTableName, blobColumnName, rowid, readOnly: true);
        }

        internal void Clear()
        {
            for (var i = 0; i < _blobCache.Length; i++)
            {
                _blobCache[i] = null;
            }
        }

        private byte[] GetCachedBlob(int ordinal)
        {
            if (ordinal < 0 || ordinal >= FieldCount)
            {
                // NB: Message is provided by the framework
                throw new ArgumentOutOfRangeException(nameof(ordinal), ordinal, message: null);
            }

            var blob = _blobCache[ordinal];
            if (blob == null)
            {
                blob = GetBlob(ordinal);
                _blobCache[ordinal] = blob;
            }

            return blob;
        }
    }
}
