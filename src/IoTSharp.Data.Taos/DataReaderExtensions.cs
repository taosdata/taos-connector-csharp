using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
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
        public static (IntPtr ptr, int len) ToUTF8IntPtr(this string command)
        {

#if NET5_0_OR_GREATER
                    IntPtr commandBuffer = Marshal.StringToCoTaskMemUTF8(command);
                    int bufferlen = Encoding.UTF8.GetByteCount(command);
#else
            var bytes = Encoding.UTF8.GetBytes(command);
            int bufferlen = bytes.Length;
            IntPtr commandBuffer = Marshal.AllocHGlobal(bufferlen);
            Marshal.Copy(bytes, 0, commandBuffer, bufferlen);
#endif
            return (commandBuffer, bufferlen);
        }

        public static void FreeUtf8IntPtr(this IntPtr ptr)
        {
#if NET5_0_OR_GREATER
            Marshal.FreeCoTaskMem(ptr);
#else
            Marshal.FreeHGlobal(ptr);
#endif
        }


    }
}
