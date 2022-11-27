using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using TDengineDriver;
namespace IoTSharp.Data.Taos
{
    public static class DataReaderExtensions
    {
        public static T ToJson<T>(this IDataReader dataReader) where T : class
        {
            return dataReader.ToJson().ToObject<T>();
        }
        public static List<T> ToList<T>(this IDataReader dataReader) where T : class
        {
            return dataReader.ToJson().ToObject<List<T>>();
        }
        public static List<T> ToObject<T>(this IDataReader dataReader)
        {
            List<T> jArray = new List<T>();
            try
            {
                var t = typeof(T);
                var pots = t.GetProperties();
                while (dataReader.Read())
                {
                    T jObject = Activator.CreateInstance<T>();
                    for (int i = 0; i < dataReader.FieldCount; i++)
                    {
                        try
                        {
                            string strKey = dataReader.GetName(i);
                            if (dataReader[i] != DBNull.Value)
                            {
                                var pr = from p in pots where (p.Name == strKey ||  p.ColumnNameIs(strKey)) && p.CanWrite select p;
                                if (pr.Any())
                                {
                                    var pi = pr.FirstOrDefault();
                                    pi.SetValue(jObject, Convert.ChangeType(dataReader[i], pi.PropertyType));
                                }
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }
                    jArray.Add(jObject);
                }
            }
            catch (Exception ex)
            {
                TaosException.ThrowExceptionForRC(-10002, $"ToObject<{nameof(T)}>  Error", ex);
            }
            return jArray;
        }

        internal static bool ColumnNameIs(this System.Reflection.PropertyInfo p, string strKey)
        {
            return (p.IsDefined(typeof(ColumnAttribute), true) && (p.GetCustomAttributes(typeof(ColumnAttribute), true) as ColumnAttribute[])?.FirstOrDefault().Name == strKey);
        }

        public static JArray ToJson(this IDataReader dataReader)
        {
            JArray jArray = new JArray();
            try
            {

                while (dataReader.Read())
                {
                    JObject jObject = new JObject();
                    for (int i = 0; i < dataReader.FieldCount; i++)
                    {
                        try
                        {
                            string strKey = dataReader.GetName(i);
                            if (dataReader[i] != DBNull.Value)
                            {
                                object obj = Convert.ChangeType(dataReader[i], dataReader.GetFieldType(i));
                                jObject.Add(strKey, JToken.FromObject(obj));
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }
                    jArray.Add(jObject);
                }
            }
            catch (Exception ex)
            {
                TaosException.ThrowExceptionForRC(-10001, "ToJson Error", ex);
            }
            return jArray;
        }
        public static DataTable ToDataTable(this IDataReader reader)
        {
            var datatable = new DataTable();
            datatable.Load(reader);
            return datatable;
        }
        public static string RemoveNull(this string str)
        {
            return str?.Trim('\0');
        }

        public static IntPtr  ToIntPtr(this long val)
        {
            IntPtr lenPtr = Marshal.AllocHGlobal(sizeof(long));
            Marshal.WriteInt64(lenPtr, val);
            return lenPtr;
        }
        public static IntPtr ToIntPtr(this int  val)
        {
            IntPtr lenPtr = Marshal.AllocHGlobal(sizeof(int ));
            Marshal.WriteInt32(lenPtr, val);
            return lenPtr;
        }
        internal struct UTF8IntPtrStruct
        {
            public IntPtr ptr;
            public int len;
        }

        internal static UTF8IntPtrStruct ToUTF8IntPtr(this string command)
        {
            UTF8IntPtrStruct result;
#if NET5_0_OR_GREATER
            IntPtr commandBuffer = Marshal.StringToCoTaskMemUTF8(command);
            int bufferlen = Encoding.UTF8.GetByteCount(command);
#else
            var bytes = Encoding.UTF8.GetBytes(command);
            int bufferlen = bytes.Length;
            IntPtr commandBuffer = Marshal.AllocHGlobal(bufferlen);
            Marshal.Copy(bytes, 0, commandBuffer, bufferlen);
#endif
            result.ptr = commandBuffer;
            result.len = bufferlen;
            return result;
        }

        public static void FreeUtf8IntPtr(this IntPtr ptr)
        {
#if NET5_0_OR_GREATER
            Marshal.FreeCoTaskMem(ptr);
#else
            Marshal.FreeHGlobal(ptr);
#endif
        }

        internal static DataTable GetSchemaTable(this TaosCommand _command, Func<int, string> getName, Func<int, Type> getFieldType, Func<int, string> getDataTypeName, Func<int, int >  getFieldSize, int column_count)
        {
            var schemaTable = new DataTable("SchemaTable");
            if (column_count > 0)
            {
                var ColumnName = new DataColumn(SchemaTableColumn.ColumnName, typeof(string));
                var ColumnOrdinal = new DataColumn(SchemaTableColumn.ColumnOrdinal, typeof(int));
                var ColumnSize = new DataColumn(SchemaTableColumn.ColumnSize, typeof(int));
                var NumericPrecision = new DataColumn(SchemaTableColumn.NumericPrecision, typeof(short));
                var NumericScale = new DataColumn(SchemaTableColumn.NumericScale, typeof(short));

                var DataType = new DataColumn(SchemaTableColumn.DataType, typeof(Type));
                var DataTypeName = new DataColumn("DataTypeName", typeof(string));

                var IsLong = new DataColumn(SchemaTableColumn.IsLong, typeof(bool));
                var AllowDBNull = new DataColumn(SchemaTableColumn.AllowDBNull, typeof(bool));

                var IsUnique = new DataColumn(SchemaTableColumn.IsUnique, typeof(bool));
                var IsKey = new DataColumn(SchemaTableColumn.IsKey, typeof(bool));
                var IsAutoIncrement = new DataColumn(SchemaTableOptionalColumn.IsAutoIncrement, typeof(bool));

                var BaseCatalogName = new DataColumn(SchemaTableOptionalColumn.BaseCatalogName, typeof(string));
                var BaseSchemaName = new DataColumn(SchemaTableColumn.BaseSchemaName, typeof(string));
                var BaseTableName = new DataColumn(SchemaTableColumn.BaseTableName, typeof(string));
                var BaseColumnName = new DataColumn(SchemaTableColumn.BaseColumnName, typeof(string));

                var BaseServerName = new DataColumn(SchemaTableOptionalColumn.BaseServerName, typeof(string));
                var IsAliased = new DataColumn(SchemaTableColumn.IsAliased, typeof(bool));
                var IsExpression = new DataColumn(SchemaTableColumn.IsExpression, typeof(bool));

                var columns = schemaTable.Columns;

                columns.Add(ColumnName);
                columns.Add(ColumnOrdinal);
                columns.Add(ColumnSize);
                columns.Add(NumericPrecision);
                columns.Add(NumericScale);
                columns.Add(IsUnique);
                columns.Add(IsKey);
                columns.Add(BaseServerName);
                columns.Add(BaseCatalogName);
                columns.Add(BaseColumnName);
                columns.Add(BaseSchemaName);
                columns.Add(BaseTableName);
                columns.Add(DataType);
                columns.Add(DataTypeName);
                columns.Add(AllowDBNull);
                columns.Add(IsAliased);
                columns.Add(IsExpression);
                columns.Add(IsAutoIncrement);
                columns.Add(IsLong);

                for (var i = 0; i < column_count; i++)
                {
                    var schemaRow = schemaTable.NewRow();

                    schemaRow[ColumnName] = getName(i);
                    schemaRow[ColumnOrdinal] = i;
                    schemaRow[ColumnSize] = getFieldSize(i);
                    schemaRow[NumericPrecision] = DBNull.Value;
                    schemaRow[NumericScale] = DBNull.Value;
                    schemaRow[BaseServerName] = _command.Connection.DataSource;
                    var databaseName = _command.Connection.Database;
                    schemaRow[BaseCatalogName] = databaseName;
                    var columnName = getName(i);
                    schemaRow[BaseColumnName] = columnName;
                    schemaRow[BaseSchemaName] = DBNull.Value;
                    var tableName = string.Empty;
                    schemaRow[BaseTableName] = tableName;
                    schemaRow[DataType] = getFieldType(i);
                    schemaRow[DataTypeName] = getDataTypeName(i);
                    schemaRow[IsAliased] = columnName != getName(i);
                    schemaRow[IsExpression] = columnName == null;
                    schemaRow[IsLong] = DBNull.Value;
                    if (i == 0)
                    {
                        schemaRow[IsKey] = true;
                        schemaRow[DataType] = getFieldType(i);
                        schemaRow[DataTypeName] = getDataTypeName(i);
                    }
                    schemaTable.Rows.Add(schemaRow);
                }
            }
            return schemaTable;
        }

        internal static bool IsUTF8Bytes(this byte[] data)
        {
            int charByteCounter = 1; //计算当前正分析的字符应还有的字节数
            byte curByte; //当前分析的字节.
            for (int i = 0; i < data.Length; i++)
            {
                curByte = data[i];
                if (charByteCounter == 1)
                {
                    if (curByte >= 0x80)
                    {
                        //判断当前
                        while (((curByte <<= 1) & 0x80) != 0)
                        {
                            charByteCounter++;
                        }
                        //标记位首位若为非0 则至少以2个1开始 如:110XXXXX…1111110X
                        if (charByteCounter == 1 || charByteCounter > 6)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    //若是UTF-8 此时第一位必须为1
                    if ((curByte & 0xC0) != 0x80)
                    {
                        return false;
                    }
                    charByteCounter--;
                }
            }
            if (charByteCounter > 1)
            {
                return false;
            }
            return true;
        }

    }
}
