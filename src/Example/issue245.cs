using IoTSharp.Data.Taos;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaosADODemo;

namespace Example
{
    internal class issue245
    {
        public static void test(TaosConnection connection)
        {

            try
            {
                connection.Open();
                connection.CreateCommand($"create database if not exists  meters;").ExecuteNonQuery();
                var lines = new List<string>();
                for (int i = 0; i < 10000; i++)
                {
                    string line = string.Format($"meters,tname=cpu{i},location=California.LosAngeles{i},groupid=2 current={11.8 + i*2},voltage={221 + i},phase={0.28+i}", Program.DateTimeToLongTimeStamp()-10000+i);
                    lines.Add(line);
                }
               connection.ChangeDatabase("meters");
                connection.ExecuteLineBulkInsert(lines.ToArray(), TDengineDriver.TDengineSchemalessPrecision.TSDB_SML_TIMESTAMP_MICRO_SECONDS);
                using var command = connection.CreateCommand();
                command.CommandText = "select * from meters limit 1000000";
                var reader = command.ExecuteReader();
                var result = reader.ToDataTable();
                Console.WriteLine(result.Rows.Count); // print 4096
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            { connection.Close(); }

        }
    }
}
