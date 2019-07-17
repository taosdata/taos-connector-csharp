// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Maikebing.Data.Taos
{
    /// <summary>
    ///     Provides methods for reading the result of a command executed against a Taos database.
    /// </summary>
    public class TaosDataReader : DbDataReader
    {
        private readonly TaosCommand _command;
        private bool _hasRows;
        private bool _closed;
        private bool _stepped;
        private bool _done;
        private readonly bool _closeConnection;
        private readonly TaosResult _taosResult  ;
        private readonly JArray _array;
        int _fieldCount;
        private IEnumerator<JToken> _record;
        internal TaosDataReader(TaosCommand taosCommand, TaosResult tr, bool closeConnection)
        {
            _command = taosCommand;
         
         
            _closeConnection = closeConnection;
            _taosResult = tr;
            var ja= _taosResult.data as JArray;
            _array = ja;
            _record = ja.AsEnumerable().GetEnumerator();
            _fieldCount = _taosResult.head.Count;
            _hasRows = ja!=null && ja.Count > 0;
            _closed = _closeConnection;
        }

        /// <summary>
        ///     Gets the depth of nesting for the current row. Always zero.
        /// </summary>
        /// <value>The depth of nesting for the current row.</value>
        public override int Depth             => 0;


  

        /// <summary>
        ///     Gets the number of columns in the current row.
        /// </summary>
        /// <value>The number of columns in the current row.</value>
        public override int FieldCount => _fieldCount;


     

        /// <summary>
        ///     Gets a value indicating whether the data reader contains any rows.
        /// </summary>
        /// <value>A value indicating whether the data reader contains any rows.</value>
        public override bool HasRows
            => _hasRows;

        /// <summary>
        ///     Gets a value indicating whether the data reader is closed.
        /// </summary>
        /// <value>A value indicating whether the data reader is closed.</value>
        public override bool IsClosed
            => _closed;

        /// <summary>
        ///     Gets the number of rows inserted, updated, or deleted. -1 for SELECT statements.
        /// </summary>
        /// <value>The number of rows inserted, updated, or deleted.</value>
        public override int RecordsAffected
        {
            get
            {
                int result = 0;
                if (_taosResult.head.Contains("affected_rows"))
                {
                    result = GetInt32(GetOrdinal("affected_rows"));
                }
                return result;
            }
        }

        /// <summary>
        ///     Gets the value of the specified column.
        /// </summary>
        /// <param name="name">The name of the column. The value is case-sensitive.</param>
        /// <returns>The value.</returns>
        public override object this[string name]
            => _record.Current[name];

        /// <summary>
        ///     Gets the value of the specified column.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value.</returns>
        public override object this[int ordinal]
            =>  _record.Current[ordinal];

        /// <summary>
        ///     Gets an enumerator that can be used to iterate through the rows in the data reader.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public override IEnumerator GetEnumerator()
            => new DbEnumerator(this, closeReader: false);

        /// <summary>
        ///     Advances to the next row in the result set.
        /// </summary>
        /// <returns>true if there are more rows; otherwise, false.</returns>
        public override bool Read()
        {
            if (_closed)
            {
                throw new InvalidOperationException($"DataReaderClosed{nameof(Read)}");
            }
            _done = _record.Current == _array.Last;
            if (!_done)
            {
                _stepped = _record.MoveNext();
            }
            if (!_stepped)
            {
                _stepped = true;
                return _hasRows;
            }
          
            return !_done;
        }

        /// <summary>
        ///     Advances to the next result set for batched statements.
        /// </summary>
        /// <returns>true if there are more result sets; otherwise, false.</returns>
        public override bool NextResult()
        {
          

            return true;
        }

        /// <summary>
        ///     Closes the data reader.
        /// </summary>
        public override void Close()
            => Dispose(true);

        /// <summary>
        ///     Releases any resources used by the data reader and closes it.
        /// </summary>
        /// <param name="disposing">
        ///     true to release managed and unmanaged resources; false to release only unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _command.DataReader = null;

        
            _closed = true;

            if (_closeConnection)
            {
                _command.Connection.Close();
            }
        }

        /// <summary>
        ///     Gets the name of the specified column.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The name of the column.</returns>
        public override string GetName(int ordinal)
        {
            return _taosResult.head[ordinal];//_recordordinal);
        }

        /// <summary>
        ///     Gets the ordinal of the specified column.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The zero-based column ordinal.</returns>
        public override int GetOrdinal(string name)
            => _taosResult.head.IndexOf(name);

 
        public override string GetDataTypeName(int ordinal)
        {
            return  _record.Current[ordinal].Type.ToString();
        }

        /// <summary>
        ///     Gets the data type of the specified column.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The data type of the column.</returns>
        public override Type GetFieldType(int ordinal)
        {
            if (_closed)
            {
                throw new InvalidOperationException($"DataReaderClosed{nameof(GetFieldType)}");
            }

            return Type.GetType( _record.Current[ordinal].Type.ToString());
        }

        /// <summary>
        ///     Gets a value indicating whether the specified column is <see cref="DBNull" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>true if the specified column is <see cref="DBNull" />; otherwise, false.</returns>
        public override bool IsDBNull(int ordinal)
                =>  _record.Current[ordinal].Type== JTokenType.Null;

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="bool" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override bool GetBoolean(int ordinal) => GetFieldValue<bool>(ordinal);

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="byte" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override byte GetByte(int ordinal) => GetFieldValue<byte>(ordinal);

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="char" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override char GetChar(int ordinal) => GetFieldValue<char>(ordinal);

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="DateTime" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override DateTime GetDateTime(int ordinal) => GetFieldValue<DateTime>(ordinal);

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="DateTimeOffset" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public virtual DateTimeOffset GetDateTimeOffset(int ordinal) => GetFieldValue<DateTimeOffset>(ordinal);

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="TimeSpan" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public virtual TimeSpan GetTimeSpan(int ordinal) => GetFieldValue<TimeSpan>(ordinal);

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="decimal" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override decimal GetDecimal(int ordinal) => GetFieldValue<decimal>(ordinal);

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="double" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override double GetDouble(int ordinal) => GetFieldValue<double>(ordinal);

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="float" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override float GetFloat(int ordinal) => GetFieldValue<float>(ordinal);

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="Guid" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override Guid GetGuid(int ordinal) => GetFieldValue<Guid>(ordinal);

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="short" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override short GetInt16(int ordinal) => GetFieldValue<short>(ordinal);

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="int" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override int GetInt32(int ordinal) => GetFieldValue<int>(ordinal);

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="long" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override long GetInt64(int ordinal) => GetFieldValue<long>(ordinal);

        /// <summary>
        ///     Gets the value of the specified column as a <see cref="string" />.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override string GetString(int ordinal)=>GetFieldValue<string>(ordinal);

        /// <summary>
        ///     Reads a stream of bytes from the specified column. Not supported.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <param name="dataOffset">The index from which to begin the read operation.</param>
        /// <param name="buffer">The buffer into which the data is copied.</param>
        /// <param name="bufferOffset">The index to which the data will be copied.</param>
        /// <param name="length">The maximum number of bytes to read.</param>
        /// <returns>The actual number of bytes read.</returns>
        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
                      => throw new  NotSupportedException();

        /// <summary>
        ///     Reads a stream of characters from the specified column. Not supported.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <param name="dataOffset">The index from which to begin the read operation.</param>
        /// <param name="buffer">The buffer into which the data is copied.</param>
        /// <param name="bufferOffset">The index to which the data will be copied.</param>
        /// <param name="length">The maximum number of characters to read.</param>
        /// <returns>The actual number of characters read.</returns>
        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
           => throw new NotSupportedException();

        /// <summary>
        ///     Retrieves data as a Stream. If the reader includes rowid (or any of its aliases), a
        ///     <see cref="TaosBlob"/> is returned. Otherwise, the all of the data is read into memory and a
        ///     <see cref="MemoryStream"/> is returned.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The returned object.</returns>
        public override Stream GetStream(int ordinal)
              => throw new NotSupportedException();

        /// <summary>
        ///     Gets the value of the specified column.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override T GetFieldValue<T>(int ordinal)
        {
            T result = default(T);
            var jt = _record.Current;
            if (jt!=null)
            {
                result= _record.Current[ordinal].Value<T>();
            }
            return result;
        }

        /// <summary>
        ///     Gets the value of the specified column.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override object GetValue(int ordinal) => GetFieldValue<object>(ordinal);


        /// <summary>
        ///     Gets the column values of the current row.
        /// </summary>
        /// <param name="values">An array into which the values are copied.</param>
        /// <returns>The number of values copied into the array.</returns>
        public override int GetValues(object[] values)
            => _record.Current.Children().Count();

        /// <summary>
        ///     Returns a System.Data.DataTable that describes the column metadata of the System.Data.Common.DbDataReader.
        /// </summary>
        /// <returns>A System.Data.DataTable that describes the column metadata.</returns>
        public override DataTable GetSchemaTable()
        {
            throw new NotSupportedException();
            //var schemaTable = new DataTable("SchemaTable");

            //var ColumnName = new DataColumn(SchemaTableColumn.ColumnName, typeof(string));
            //var ColumnOrdinal = new DataColumn(SchemaTableColumn.ColumnOrdinal, typeof(int));
            //var ColumnSize = new DataColumn(SchemaTableColumn.ColumnSize, typeof(int));
            //var NumericPrecision = new DataColumn(SchemaTableColumn.NumericPrecision, typeof(short));
            //var NumericScale = new DataColumn(SchemaTableColumn.NumericScale, typeof(short));

            //var DataType = new DataColumn(SchemaTableColumn.DataType, typeof(Type));
            //var DataTypeName = new DataColumn("DataTypeName", typeof(string));

            //var IsLong = new DataColumn(SchemaTableColumn.IsLong, typeof(bool));
            //var AllowDBNull = new DataColumn(SchemaTableColumn.AllowDBNull, typeof(bool));

            //var IsUnique = new DataColumn(SchemaTableColumn.IsUnique, typeof(bool));
            //var IsKey = new DataColumn(SchemaTableColumn.IsKey, typeof(bool));
            //var IsAutoIncrement = new DataColumn(SchemaTableOptionalColumn.IsAutoIncrement, typeof(bool));

            //var BaseCatalogName = new DataColumn(SchemaTableOptionalColumn.BaseCatalogName, typeof(string));
            //var BaseSchemaName = new DataColumn(SchemaTableColumn.BaseSchemaName, typeof(string));
            //var BaseTableName = new DataColumn(SchemaTableColumn.BaseTableName, typeof(string));
            //var BaseColumnName = new DataColumn(SchemaTableColumn.BaseColumnName, typeof(string));

            //var BaseServerName = new DataColumn(SchemaTableOptionalColumn.BaseServerName, typeof(string));
            //var IsAliased = new DataColumn(SchemaTableColumn.IsAliased, typeof(bool));
            //var IsExpression = new DataColumn(SchemaTableColumn.IsExpression, typeof(bool));

            //var columns = schemaTable.Columns;

            //columns.Add(ColumnName);
            //columns.Add(ColumnOrdinal);
            //columns.Add(ColumnSize);
            //columns.Add(NumericPrecision);
            //columns.Add(NumericScale);
            //columns.Add(IsUnique);
            //columns.Add(IsKey);
            //columns.Add(BaseServerName);
            //columns.Add(BaseCatalogName);
            //columns.Add(BaseColumnName);
            //columns.Add(BaseSchemaName);
            //columns.Add(BaseTableName);
            //columns.Add(DataType);
            //columns.Add(DataTypeName);
            //columns.Add(AllowDBNull);
            //columns.Add(IsAliased);
            //columns.Add(IsExpression);
            //columns.Add(IsAutoIncrement);
            //columns.Add(IsLong);

            //for (var i = 0; i < FieldCount; i++)
            //{
            //    var schemaRow = schemaTable.NewRow();
            //    schemaRow[ColumnName] = GetName(i);
            //    schemaRow[ColumnOrdinal] = i;
            //    schemaRow[ColumnSize] = DBNull.Value;
            //    schemaRow[NumericPrecision] = DBNull.Value;
            //    schemaRow[NumericScale] = DBNull.Value;
            //    schemaRow[BaseServerName] = _command.Connection.DataSource;
            //    var databaseName =_command.Connection.Database;
            //    schemaRow[BaseCatalogName] = databaseName;
            //    var columnName = GetName(i);
            //    schemaRow[BaseColumnName] = columnName;
            //    schemaRow[BaseSchemaName] = DBNull.Value;
            //    var tableName = string.Empty;
            //    schemaRow[BaseTableName] = tableName;
            //    schemaRow[DataType] = GetFieldType(i);
            //    schemaRow[DataTypeName] = GetDataTypeName(i);
            //    schemaRow[IsAliased] = columnName != GetName(i);
            //    schemaRow[IsExpression] = columnName == null;
            //    schemaRow[IsLong] = DBNull.Value;

            //    if (!string.IsNullOrEmpty(tableName)
            //        && !string.IsNullOrEmpty(columnName))
            //    {
            //        using (var command = _command.Connection.CreateCommand())
            //        {
            //            command.CommandText = new StringBuilder()
            //                .AppendLine("SELECT COUNT(*)")
            //                .AppendLine("FROM pragma_index_list($table) i, pragma_index_info(i.name) c")
            //                .AppendLine("WHERE \"unique\" = 1 AND c.name = $column AND")
            //                .AppendLine("NOT EXISTS (SELECT * FROM pragma_index_info(i.name) c2 WHERE c2.name != c.name);").ToString();
            //            command.Parameters.AddWithValue("$table", tableName);
            //            command.Parameters.AddWithValue("$column", columnName);

            //            var cnt = (long)command.ExecuteScalar();
            //            schemaRow[IsUnique] = cnt != 0;

            //            command.Parameters.Clear();
            //            var columnType = "typeof(\"" + columnName.Replace("\"", "\"\"") + "\")";
            //            command.CommandText = new StringBuilder()
            //                .AppendLine($"SELECT {columnType}")
            //                .AppendLine($"FROM \"{tableName}\"")
            //                .AppendLine($"WHERE {columnType} != 'null'")
            //                .AppendLine($"GROUP BY {columnType}")
            //                .AppendLine("ORDER BY count() DESC")
            //                .AppendLine("LIMIT 1;").ToString();

            //            var type = (string)command.ExecuteScalar();
            //            schemaRow[DataType] = TaosDataRecord.GetFieldType(type);
            //        }

            //        if (!string.IsNullOrEmpty(databaseName))
            //        {
            //            var rc = raw.Taos3_table_column_metadata(_command.Connection.Handle, databaseName, tableName, columnName, out var dataType, out var collSeq, out var notNull, out var primaryKey, out var autoInc);
            //            TaosException.ThrowExceptionForRC(rc, _command.Connection.Handle);

            //            schemaRow[IsKey] = primaryKey != 0;
            //            schemaRow[AllowDBNull] = notNull == 0;
            //            schemaRow[IsAutoIncrement] = autoInc != 0;
            //        }
            //    }

            //    schemaTable.Rows.Add(schemaRow);
            //}

            //return schemaTable;

        }
    }
}
