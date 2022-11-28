using ConsoleTableExt;
using IoTSharp.Data.Taos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TDengineDriver;

namespace TaosADODemo
{
    class Program
    {
        /// <summary>
        /// 时间戳计时开始时间
        /// </summary>
        private static DateTime timeStampStartTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// DateTime转换为13位时间戳（单位：毫秒）
        /// </summary>
        /// <param name="dateTime"> DateTime</param>
        /// <returns>13位时间戳（单位：毫秒）</returns>
        public static long DateTimeToLongTimeStamp()
        {
            return (long)(DateTime.Now.ToUniversalTime() - timeStampStartTime).TotalMilliseconds;
        }

        static void Main(string[] args)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            DbProviderFactories.RegisterFactory("TDengine", TaosFactory.Instance);
            ///Specify the name of the database
            string database = "db_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var builder = new TaosConnectionStringBuilder()
            {
                DataSource = "taos",
                DataBase = database,
                Username = "root",
                Password = "taosdata",
                Port = 6030

            };
            ExecSqlByWebSocket(database);
            ExecSqlByRESTFul(database);

            string configdir = System.Environment.CurrentDirectory.Replace("\\", "/") + "/";
            using (var connection = new TaosConnection(builder.ConnectionString, configdir))
            {
                try
                {
                    connection.Open();
                    Console.WriteLine("create {0} {1}", database, connection.CreateCommand($"create database {database};").ExecuteNonQuery());
                    connection.ChangeDatabase(database);
                    var lines = new string[] {
                        String.Format("meters,tname=cpu1,location=California.LosAngeles,groupid=2 current=11.8,voltage=221,phase=0.28 {0}",DateTimeToLongTimeStamp()),
                };

                    int result = connection.ExecuteBulkInsert(lines);
                    Console.WriteLine($"行插入{result}");
                    if (result != lines.Length)
                    {
                        throw new Exception("ExecuteBulkInsert");
                    }
                    var cmd_select = connection.CreateCommand();
                    cmd_select.CommandText = $"select * from {database}.cpu1;";
                    var reader = cmd_select.ExecuteReader();
                    Console.WriteLine("");
                    ConsoleTableBuilder.From(reader.ToDataTable()).WithFormat(ConsoleTableBuilderFormat.MarkDown).ExportAndWriteLine();
                    connection.CreateCommand($"DROP DATABASE {database};").ExecuteNonQuery();

                    Console.WriteLine("执行TDengine成功");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("执行TDengine异常" + ex.Message);
                }
                finally
                {
                    connection.Close();
                }
            }

            //Example for ADO.Net 
            using (var connection = new TaosConnection(builder.ConnectionString))
            {
                connection.Open();
                Console.WriteLine("ServerVersion:{0}", connection.ServerVersion);
                connection.DatabaseExists(database);
                Console.WriteLine("create {0} {1}", database, connection.CreateCommand($"create database {database};").ExecuteNonQuery());
                Console.WriteLine("create table t {0} {1}", database, connection.CreateCommand($"create table {database}.t (ts timestamp, cdata binary(255));").ExecuteNonQuery());
                Console.WriteLine("insert into t values  {0}  ", connection.CreateCommand($"insert into {database}.t values ({(long)(DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds)}, 10);").ExecuteNonQuery());
                Console.WriteLine("create {0} {1}", database, connection.CreateCommand($"use {database};").ExecuteNonQuery());

                Console.WriteLine("单表插入一行数据  {0}  ", connection.CreateCommand($"insert into {database}.t values ({(long)(DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds)}, 10);").ExecuteNonQuery());
                Thread.Sleep(100);
                Console.WriteLine("单表插入多行数据 {0}  ", connection.CreateCommand($"insert into {database}.t values ({(long)(DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds)}, 101) ({(long)(DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds + 1)}, 992);").ExecuteNonQuery());
                connection.ChangeDatabase(database);
                BindParamBatch(connection);

                Console.WriteLine("insert into t values  {0} ", connection.CreateCommand($"insert into {database}.t values ({(long)(DateTime.Now.AddMonths(-1).Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds)}, 20);").ExecuteNonQuery());
                var cmd_select = connection.CreateCommand();
                cmd_select.CommandText = $"select * from {database}.t;";
                var reader = cmd_select.ExecuteReader();

                int index = reader.GetOrdinal("cdata");
                if (reader.Read())
                {
                    var ts = reader.GetDateTime("ts");
                }
                Console.WriteLine($"cdata index at {index}");
                Console.WriteLine(cmd_select.CommandText);
                Console.WriteLine("");
                ConsoleTableBuilder.From(reader.ToDataTable()).WithFormat(ConsoleTableBuilderFormat.MarkDown).ExportAndWriteLine();

                connection.ChangeDatabase(database);
                Console.WriteLine("");
                connection.CreateCommand($"CREATE TABLE datas (reportTime timestamp, type int, bufferedEnd bool, address nchar(64), parameter nchar(64), `value` nchar(64)) TAGS (boxCode nchar(64), machineId int);").ExecuteNonQuery();
                connection.CreateCommand($"INSERT INTO  data_history_67 USING datas TAGS ('mongo', 67) values ( 1608173534840, 2 ,false ,'Channel1.窑.烟囱温度', '烟囱温度' ,'122.00' );").ExecuteNonQuery();
                var cmd_datas = connection.CreateCommand();
                cmd_datas.CommandText = $"SELECT  reportTime,type,bufferedEnd,address,parameter,`value` FROM  {database}.data_history_67 LIMIT  100";
                var readerdatas = cmd_datas.ExecuteReader();
                Console.WriteLine(cmd_datas.CommandText);
                Console.WriteLine("");
                ConsoleTableBuilder.From(readerdatas.ToDataTable()).WithFormat(ConsoleTableBuilderFormat.Default).ExportAndWriteLine();
                Console.WriteLine("");

                Console.WriteLine("CREATE TABLE meters ", connection.CreateCommand($"CREATE TABLE meters (ts timestamp, current float, voltage int, phase float) TAGS (location binary(64), groupdId int);").ExecuteNonQuery());
                Console.WriteLine("CREATE TABLE d1001 ", connection.CreateCommand($"CREATE TABLE d1001 USING meters TAGS (\"Beijing.Chaoyang\", 2);").ExecuteNonQuery());
                Console.WriteLine("INSERT INTO d1001  ", connection.CreateCommand($"INSERT INTO d1001 USING METERS TAGS(\"Beijng.Chaoyang\", 2) VALUES(now, 10.2, 219, 0.32);").ExecuteNonQuery());
                Console.WriteLine("DROP TABLE  {0} {1}", database, connection.CreateCommand($"DROP TABLE  {database}.t;").ExecuteNonQuery());
                Console.WriteLine("DROP DATABASE {0} {1}", database, connection.CreateCommand($"DROP DATABASE   {database};").ExecuteNonQuery());
                connection.CreateCommand("DROP DATABASE IF EXISTS  IoTSharp").ExecuteNonQuery();
                connection.CreateCommand("CREATE DATABASE IoTSharp KEEP 365d;").ExecuteNonQuery();
                connection.ChangeDatabase("IoTSharp");
                connection.CreateCommand("CREATE TABLE IF NOT EXISTS telemetrydata  (ts timestamp,value_type  tinyint, value_boolean bool, value_string binary(10240), value_long bigint,value_datetime timestamp,value_double double)   TAGS (deviceid binary(32),keyname binary(64));").ExecuteNonQuery();
                var devid1 = $"{Guid.NewGuid():N}";
                var devid2 = $"{Guid.NewGuid():N}";
                var devid3 = $"{Guid.NewGuid():N}";
                var devid4 = $"{Guid.NewGuid():N}";
                DateTime dt = DateTime.Now;
                UploadTelemetryData(connection, devid1, "1#air-compressor-two-level-discharge-temperature", 1000);
                UploadTelemetryData(connection, devid2, "1#air-compressor-load-rate", 1000);
                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine("");
                DateTime dt2 = DateTime.Now;
                UploadTelemetryDataPool(connection, devid3, "1#air-compressor-two-level-discharge-temperature1", 1000);
                UploadTelemetryDataPool(connection, devid4, "1#air-compressor-load-rate1", 1000);
                var t1 = DateTime.Now.Subtract(dt).TotalSeconds;
                var t2 = DateTime.Now.Subtract(dt2).TotalSeconds;
                Console.WriteLine("Done");
                Thread.Sleep(TimeSpan.FromSeconds(1));
                Console.WriteLine($"UploadTelemetryData 耗时:{t1}");
                Console.WriteLine($"UploadTelemetryDataPool 耗时:{t2}");
                Thread.Sleep(TimeSpan.FromSeconds(2));
                var reader2 = connection.CreateCommand("select last_row(*) from telemetrydata group by deviceid,keyname ;").ExecuteReader();
                ConsoleTableBuilder.From(reader2.ToDataTable()).WithFormat(ConsoleTableBuilderFormat.Default).ExportAndWriteLine();
                var reader3 = connection.CreateCommand("select * from telemetrydata;").ExecuteReader();
                List<string> list = new List<string>();
                while (reader3.Read())
                {
                    list.Add(reader3.GetString("keyname"));
                }

                var k = list.GroupBy(e => e);
                var dic = k.ToDictionary(en => en.Key, en => en.ToList());
                dic.Keys.ToList().ForEach(k =>
                {
                    Console.WriteLine(k);
                    ConsoleTableBuilder.From(dic[k]).WithFormat(ConsoleTableBuilderFormat.Default).ExportAndWriteLine(TableAligntment.Center);
                });


                //行插入
                BulkInsertLines(connection);
                //使用对象构建Lines 航插入
                BulkRecordData(connection);
                //使用json插入
                BulkInsertJsonString(connection);

                BulkInsertByJsonAndTags(connection);

                BulkInsertWithJson_BuildJson(connection);

                Console.WriteLine("DROP DATABASE IoTSharp", database, connection.CreateCommand($"DROP DATABASE IoTSharp;").ExecuteNonQuery());



                connection.Close();



            }
            UseTaosEFCore(builder);

        }
        private static void ExecSqlByWebSocket(string database)
        {
            var builder_rest = new TaosConnectionStringBuilder()
            {
                DataSource = "localhost",
                DataBase = database,
                Username = "root",
                Password = "taosdata",

            }.UseWebSocket();
            using (var connection = new TaosConnection(builder_rest.ConnectionString))
            {
                connection.Open();
                Console.WriteLine("ServerVersion:{0}", connection.ServerVersion);
                connection.DatabaseExists(database);
                Console.WriteLine("create {0} {1}", database, connection.CreateCommand($"create database {database};").ExecuteNonQuery());
                Console.WriteLine("create table t {0} {1}", database, connection.CreateCommand($"create table {database}.t (ts timestamp, cdata binary(255));").ExecuteNonQuery());
                Console.WriteLine("insert into t values  {0}  ", connection.CreateCommand($"insert into {database}.t values ({(long)(DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds)}, 10);").ExecuteNonQuery());
                Console.WriteLine("create {0} {1}", database, connection.CreateCommand($"use {database};").ExecuteNonQuery());

                Console.WriteLine("单表插入一行数据  {0}  ", connection.CreateCommand($"insert into {database}.t values ({(long)(DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds)}, 10);").ExecuteNonQuery());
                Thread.Sleep(100);
                Console.WriteLine("单表插入多行数据 {0}  ", connection.CreateCommand($"insert into {database}.t values ({(long)(DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds)}, 101) ({(long)(DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds + 1)}, 992);").ExecuteNonQuery());
                connection.ChangeDatabase(database);


                Console.WriteLine("insert into t values  {0} ", connection.CreateCommand($"insert into {database}.t values ({(long)(DateTime.Now.AddMonths(-1).Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds)}, 20);").ExecuteNonQuery());
                var cmd_select = connection.CreateCommand();
                cmd_select.CommandText = $"select * from {database}.t;";
                var reader = cmd_select.ExecuteReader();

                int index = reader.GetOrdinal("cdata");
                if (reader.Read())
                {
                    var ts = reader.GetDateTime("ts");
                }
                Console.WriteLine($"cdata index at {index}");
                Console.WriteLine(cmd_select.CommandText);
                Console.WriteLine("");
                ConsoleTableBuilder.From(reader.ToDataTable()).WithFormat(ConsoleTableBuilderFormat.MarkDown).ExportAndWriteLine();

                connection.ChangeDatabase(database);
                Console.WriteLine("");
                connection.CreateCommand($"CREATE TABLE datas (reportTime timestamp, type int, bufferedEnd bool, address nchar(64), parameter nchar(64), `value` nchar(64)) TAGS (boxCode nchar(64), machineId int);").ExecuteNonQuery();
                connection.CreateCommand($"INSERT INTO  data_history_67 USING datas TAGS ('mongo', 67) values ( 1608173534840, 2 ,false ,'Channel1.窑.烟囱温度', '烟囱温度' ,'122.00' );").ExecuteNonQuery();
                var cmd_datas = connection.CreateCommand();
                cmd_datas.CommandText = $"SELECT  reportTime,type,bufferedEnd,address,parameter,`value` FROM  {database}.data_history_67 LIMIT  100";
                var readerdatas = cmd_datas.ExecuteReader();
                Console.WriteLine(cmd_datas.CommandText);
                Console.WriteLine("");
                ConsoleTableBuilder.From(readerdatas.ToDataTable()).WithFormat(ConsoleTableBuilderFormat.Default).ExportAndWriteLine();
                Console.WriteLine("");

                Console.WriteLine("CREATE TABLE meters ", connection.CreateCommand($"CREATE TABLE meters (ts timestamp, current float, voltage int, phase float) TAGS (location binary(64), groupdId int);").ExecuteNonQuery());
                Console.WriteLine("CREATE TABLE d1001 ", connection.CreateCommand($"CREATE TABLE d1001 USING meters TAGS (\"Beijing.Chaoyang\", 2);").ExecuteNonQuery());
                Console.WriteLine("INSERT INTO d1001  ", connection.CreateCommand($"INSERT INTO d1001 USING METERS TAGS(\"Beijng.Chaoyang\", 2) VALUES(now, 10.2, 219, 0.32);").ExecuteNonQuery());
                Console.WriteLine("DROP TABLE  {0} {1}", database, connection.CreateCommand($"DROP TABLE  {database}.t;").ExecuteNonQuery());
                Console.WriteLine("DROP DATABASE {0} {1}", database, connection.CreateCommand($"DROP DATABASE   {database};").ExecuteNonQuery());
                connection.CreateCommand("DROP DATABASE IF EXISTS  IoTSharp").ExecuteNonQuery();
                connection.CreateCommand("CREATE DATABASE IoTSharp KEEP 365d;").ExecuteNonQuery();
                connection.ChangeDatabase("IoTSharp");
                connection.CreateCommand("CREATE TABLE IF NOT EXISTS telemetrydata  (ts timestamp,value_type  tinyint, value_boolean bool, value_string binary(10240), value_long bigint,value_datetime timestamp,value_double double)   TAGS (deviceid binary(32),keyname binary(64));").ExecuteNonQuery();
                var devid1 = $"{Guid.NewGuid():N}";
                var devid2 = $"{Guid.NewGuid():N}";
                var devid3 = $"{Guid.NewGuid():N}";
                var devid4 = $"{Guid.NewGuid():N}";
                DateTime dt = DateTime.Now;
                UploadTelemetryData(connection, devid1, "1#air-compressor-two-level-discharge-temperature", 1000);
                UploadTelemetryData(connection, devid2, "1#air-compressor-load-rate", 1000);
                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine("");
                DateTime dt2 = DateTime.Now;
                UploadTelemetryDataPool(connection, devid3, "1#air-compressor-two-level-discharge-temperature1", 1000);
                UploadTelemetryDataPool(connection, devid4, "1#air-compressor-load-rate1", 1000);
                var t1 = DateTime.Now.Subtract(dt).TotalSeconds;
                var t2 = DateTime.Now.Subtract(dt2).TotalSeconds;
                Console.WriteLine("Done");
                Thread.Sleep(TimeSpan.FromSeconds(1));
                Console.WriteLine($"UploadTelemetryData 耗时:{t1}");
                Console.WriteLine($"UploadTelemetryDataPool 耗时:{t2}");
                Thread.Sleep(TimeSpan.FromSeconds(2));
                var reader2 = connection.CreateCommand("select last_row(*) from telemetrydata group by deviceid,keyname ;").ExecuteReader();
                ConsoleTableBuilder.From(reader2.ToDataTable()).WithFormat(ConsoleTableBuilderFormat.Default).ExportAndWriteLine();
                var reader3 = connection.CreateCommand("select * from telemetrydata;").ExecuteReader();
                List<string> list = new List<string>();
                while (reader3.Read())
                {
                    list.Add(reader3.GetString("keyname"));
                }

                var k = list.GroupBy(e => e);
                var dic = k.ToDictionary(en => en.Key, en => en.ToList());
                dic.Keys.ToList().ForEach(k =>
                {
                    Console.WriteLine(k);
                    ConsoleTableBuilder.From(dic[k]).WithFormat(ConsoleTableBuilderFormat.Default).ExportAndWriteLine(TableAligntment.Center);
                });



                Console.WriteLine("DROP DATABASE IoTSharp", database, connection.CreateCommand($"DROP DATABASE IoTSharp;").ExecuteNonQuery());



                connection.Close();



            }
        }
        private static void ExecSqlByRESTFul(string database)
        {
            var builder_rest = new TaosConnectionStringBuilder()
            {
                DataSource = "taos",
                DataBase = database,
                Username = "root",
                Password = "taosdata",

            }.UseRESTful();
            using (var connection = new TaosConnection(builder_rest.ConnectionString))
            {
                connection.Open();
                Console.WriteLine("ServerVersion:{0}", connection.ServerVersion);
                connection.DatabaseExists(database);
                Console.WriteLine("create {0} {1}", database, connection.CreateCommand($"create database {database};").ExecuteNonQuery());
                Console.WriteLine("create table t {0} {1}", database, connection.CreateCommand($"create table {database}.t (ts timestamp, cdata binary(255));").ExecuteNonQuery());
                Console.WriteLine("insert into t values  {0}  ", connection.CreateCommand($"insert into {database}.t values ({(long)(DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds)}, 10);").ExecuteNonQuery());
                Console.WriteLine("create {0} {1}", database, connection.CreateCommand($"use {database};").ExecuteNonQuery());

                Console.WriteLine("单表插入一行数据  {0}  ", connection.CreateCommand($"insert into {database}.t values ({(long)(DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds)}, 10);").ExecuteNonQuery());
                Thread.Sleep(100);
                Console.WriteLine("单表插入多行数据 {0}  ", connection.CreateCommand($"insert into {database}.t values ({(long)(DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds)}, 101) ({(long)(DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds + 1)}, 992);").ExecuteNonQuery());
                connection.ChangeDatabase(database);


                Console.WriteLine("insert into t values  {0} ", connection.CreateCommand($"insert into {database}.t values ({(long)(DateTime.Now.AddMonths(-1).Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds)}, 20);").ExecuteNonQuery());
                var cmd_select = connection.CreateCommand();
                cmd_select.CommandText = $"select * from {database}.t;";
                var reader = cmd_select.ExecuteReader();

                int index = reader.GetOrdinal("cdata");
                if (reader.Read())
                {
                    var ts = reader.GetDateTime("ts");
                }
                Console.WriteLine($"cdata index at {index}");
                Console.WriteLine(cmd_select.CommandText);
                Console.WriteLine("");
                ConsoleTableBuilder.From(reader.ToDataTable()).WithFormat(ConsoleTableBuilderFormat.MarkDown).ExportAndWriteLine();

                connection.ChangeDatabase(database);
                Console.WriteLine("");
                connection.CreateCommand($"CREATE TABLE datas (reportTime timestamp, type int, bufferedEnd bool, address nchar(64), parameter nchar(64), `value` nchar(64)) TAGS (boxCode nchar(64), machineId int);").ExecuteNonQuery();
                connection.CreateCommand($"INSERT INTO  data_history_67 USING datas TAGS ('mongo', 67) values ( 1608173534840, 2 ,false ,'Channel1.窑.烟囱温度', '烟囱温度' ,'122.00' );").ExecuteNonQuery();
                var cmd_datas = connection.CreateCommand();
                cmd_datas.CommandText = $"SELECT  reportTime,type,bufferedEnd,address,parameter,`value` FROM  {database}.data_history_67 LIMIT  100";
                var readerdatas = cmd_datas.ExecuteReader();
                Console.WriteLine(cmd_datas.CommandText);
                Console.WriteLine("");
                ConsoleTableBuilder.From(readerdatas.ToDataTable()).WithFormat(ConsoleTableBuilderFormat.Default).ExportAndWriteLine();
                Console.WriteLine("");

                Console.WriteLine("CREATE TABLE meters ", connection.CreateCommand($"CREATE TABLE meters (ts timestamp, current float, voltage int, phase float) TAGS (location binary(64), groupdId int);").ExecuteNonQuery());
                Console.WriteLine("CREATE TABLE d1001 ", connection.CreateCommand($"CREATE TABLE d1001 USING meters TAGS (\"Beijing.Chaoyang\", 2);").ExecuteNonQuery());
                Console.WriteLine("INSERT INTO d1001  ", connection.CreateCommand($"INSERT INTO d1001 USING METERS TAGS(\"Beijng.Chaoyang\", 2) VALUES(now, 10.2, 219, 0.32);").ExecuteNonQuery());
                Console.WriteLine("DROP TABLE  {0} {1}", database, connection.CreateCommand($"DROP TABLE  {database}.t;").ExecuteNonQuery());
                Console.WriteLine("DROP DATABASE {0} {1}", database, connection.CreateCommand($"DROP DATABASE   {database};").ExecuteNonQuery());
                connection.CreateCommand("DROP DATABASE IF EXISTS  IoTSharp").ExecuteNonQuery();
                connection.CreateCommand("CREATE DATABASE IoTSharp KEEP 365d;").ExecuteNonQuery();
                connection.ChangeDatabase("IoTSharp");
                connection.CreateCommand("CREATE TABLE IF NOT EXISTS telemetrydata  (ts timestamp,value_type  tinyint, value_boolean bool, value_string binary(10240), value_long bigint,value_datetime timestamp,value_double double)   TAGS (deviceid binary(32),keyname binary(64));").ExecuteNonQuery();
                var devid1 = $"{Guid.NewGuid():N}";
                var devid2 = $"{Guid.NewGuid():N}";
                var devid3 = $"{Guid.NewGuid():N}";
                var devid4 = $"{Guid.NewGuid():N}";
                DateTime dt = DateTime.Now;
                UploadTelemetryData(connection, devid1, "1#air-compressor-two-level-discharge-temperature", 1000);
                UploadTelemetryData(connection, devid2, "1#air-compressor-load-rate", 1000);
                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine("");
                DateTime dt2 = DateTime.Now;
                UploadTelemetryDataPool(connection, devid3, "1#air-compressor-two-level-discharge-temperature1", 1000);
                UploadTelemetryDataPool(connection, devid4, "1#air-compressor-load-rate1", 1000);
                var t1 = DateTime.Now.Subtract(dt).TotalSeconds;
                var t2 = DateTime.Now.Subtract(dt2).TotalSeconds;
                Console.WriteLine("Done");
                Thread.Sleep(TimeSpan.FromSeconds(1));
                Console.WriteLine($"UploadTelemetryData 耗时:{t1}");
                Console.WriteLine($"UploadTelemetryDataPool 耗时:{t2}");
                Thread.Sleep(TimeSpan.FromSeconds(2));
                var reader2 = connection.CreateCommand("select last_row(*) from telemetrydata group by deviceid,keyname ;").ExecuteReader();
                ConsoleTableBuilder.From(reader2.ToDataTable()).WithFormat(ConsoleTableBuilderFormat.Default).ExportAndWriteLine();
                var reader3 = connection.CreateCommand("select * from telemetrydata;").ExecuteReader();
                List<string> list = new List<string>();
                while (reader3.Read())
                {
                    list.Add(reader3.GetString("keyname"));
                }

                var k = list.GroupBy(e => e);
                var dic = k.ToDictionary(en => en.Key, en => en.ToList());
                dic.Keys.ToList().ForEach(k =>
                {
                    Console.WriteLine(k);
                    ConsoleTableBuilder.From(dic[k]).WithFormat(ConsoleTableBuilderFormat.Default).ExportAndWriteLine(TableAligntment.Center);
                });



                Console.WriteLine("DROP DATABASE IoTSharp", database, connection.CreateCommand($"DROP DATABASE IoTSharp;").ExecuteNonQuery());



                connection.Close();



            }
        }

        private static void UseTaosEFCore(TaosConnectionStringBuilder builder)
        {
            //Example for  Entity Framework Core  
            using (var context = new TaosContext(new DbContextOptionsBuilder()
                                                    .UseTaos(builder.ConnectionString).Options))
            {
                Console.WriteLine("EnsureCreated");
                context.Database.EnsureCreated();
                for (int i = 0; i < 10; i++)
                {
                    var rd = new Random();
                    context.sensor.Add(new sensor() { ts = DateTime.Now.AddMilliseconds(i + 10), degree = rd.NextDouble(), pm25 = rd.Next(0, 1000) });
                    Thread.Sleep(10);
                }
                Console.WriteLine("Saveing");
                context.SaveChanges();
                Console.WriteLine("");
                Console.WriteLine("from s in context.sensor where s.pm25 > 0 select s ");
                Console.WriteLine("");
                var f = from s in context.sensor where s.pm25 > 0 select s;
                var ary = f.ToArray();
                if (ary.Any())
                {
                    ConsoleTableBuilder.From(ary.ToList()).WithFormat(ConsoleTableBuilderFormat.MarkDown).ExportAndWriteLine();
                }
                context.Database.EnsureDeleted();
            }
        }

        private static void BulkInsertWithJson_BuildJson(TaosConnection connection)
        {
            var payload = new JObject();
            var tags = new JObject();
            payload.Add("metric", "stb3_0");

            var timestamp = new JObject
                {
                    { "value", DateTimeOffset.Now.ToUnixTimeSeconds() },
                    { "type", "s" }
                };
            payload.Add("timestamp", timestamp);

            var metric_val = new JObject
                {
                    { "value", "hello" },
                    { "type", "nchar" }
                };
            payload.Add("value", metric_val);
            AddTag(tags, "t1", true, "bool");
            AddTag(tags, "t2", false, "bool");
            AddTag(tags, "t3", 127, "tinyint");
            AddTag(tags, "t4", 32767, "smallint");
            AddTag(tags, "t5", 127, "2147483647");
            AddTag(tags, "t6", (double)9223372036854775807, "bigint");
            AddTag(tags, "t7", 11.12345, "float");
            AddTag(tags, "t8", 22.1234567890, "double");
            AddTag(tags, "t9", "binary_val", "binary");
            AddTag(tags, "t10", "你好", "nchar");
            payload.Add("tags", tags);

            int resultjson3 = connection.ExecuteBulkInsert(new JObject[] { payload });
            Console.WriteLine($"行插入{resultjson3}");
            if (resultjson3 != 1)
            {
                throw new Exception("ExecuteBulkInsert");
            }
        }

        private static JObject AddTag(JObject tags, string name, object value, string type)
        {
            var tag = new JObject
                    {
                        { "value", true },
                        { "type", "bool" }
                    };
            tags.Add(name, tag);
            return tag;
        }

        private static void BulkInsertByJsonAndTags(TaosConnection connection)
        {
            var jo = new JObject
                {
                    { "metric", "stb0_0" },
                    { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },// 1626006833610);
                    { "value", 10 }
                };
            var tags1 = new JObject
                {
                    { "t1", true },
                    { "t2", false },
                    { "t3", 10 },
                    { "t4", "123_abc_.!@#$%^&*:;,./?|+-=()[]{}<>" }
                };
            jo.Add("tags", tags1);
            int resultjson2 = connection.ExecuteBulkInsert(new JObject[] { jo }, TDengineDriver.TDengineSchemalessPrecision.TSDB_SML_TIMESTAMP_NOT_CONFIGURED);
            Console.WriteLine($"行插入{resultjson2}");
        }

        private static void BulkInsertJsonString(TaosConnection connection)
        {
            string[] jsonStr = {
                "{"
                   +"\"metric\": \"stb0_0\","
                   +$"\"timestamp\": {DateTimeOffset.Now.ToUnixTimeSeconds()},"
                   +"\"value\": 10,"
                   +"\"tags\": {"
                       +" \"t1\": true,"
                       +"\"t2\": false,"
                       +"\"t3\": 10,"
                       +"\"t4\": \"123_abc_.!@#$%^&*:;,./?|+-=()[]{}<>\""
                    +"}"
                +"}"
            };
            int resultjson = connection.ExecuteBulkInsert(JArray.FromObject(jsonStr.Select(j => JObject.Parse(j)).ToArray()));
            Console.WriteLine($"Json插入{resultjson}");
        }

        private static void BulkInsertLines(TaosConnection connection)
        {
          
           var lines =new string[] {
                "meters,location=Beijing.Haidian,groupid=2 current=11.8,voltage=221,phase=0.28 1648432611249",
                "meters,location=Beijing.Haidian,groupid=2 current=13.4,voltage=223,phase=0.29 1648432611250",
                "meters,location=Beijing.Haidian,groupid=3 current=10.8,voltage=223,phase=0.29 1648432611249",
                "meters,location=Beijing.Haidian,groupid=3 current=11.3,voltage=221,phase=0.35 1648432611250"
            };
            int  result = connection.ExecuteBulkInsert(lines);
            Console.WriteLine($"行插入{result}");
            if (result != lines.Length)
            {
                throw new Exception("ExecuteBulkInsert");
            }
        }
        /// <summary>
        /// taos_schemaless_insert 数值类型 和 时间精度 #18413
        /// </summary>
        /// <param name="connection"></param>
        /// <exception cref="Exception"></exception>
        /// <seealso cref="https://github.com/taosdata/TDengine/issues/18413"/>
        private static void BulkRecordData(TaosConnection connection)
        {
           var rec=  RecordData.table("meters").Tag("location", "Beijing.Haidian").Tag("groupid", "2").Timestamp(DateTime.Now.ToUniversalTime(), TimePrecision.Ms)
                .Field("current", 12.1).Field("voltage", 234.0).Field("phase",0.33);
            int result = connection.ExecuteBulkInsert(rec);
            Console.WriteLine($"行插入{result}");
            if (result != 1)
            {
                throw new Exception("ExecuteBulkInsert");
            }
        }

        private static void BindParamBatch(TaosConnection connection)
        {
            var stable = "bind_param_batch";
            string createTable = $"create stable if not exists {stable} (ts timestamp "
                        + ",b bool"
                        + ",v1 tinyint"
                        + ",v2 smallint"
                        + ",v4 int"
                        + ",v8 bigint"
                        + ",f4 float"
                        + ",f8 double"
                        + ",u1 tinyint unsigned"
                        + ",u2 smallint unsigned"
                        + ",u4 int unsigned"
                        + ",u8 bigint unsigned"
                        + ",vcr varchar(200)"
                        + ",ncr nchar(200)"
                        + ")tags("
                        + "bo bool"
                         + ",tt tinyint"
                         + ",si smallint"
                         + ",ii int"
                         + ",bi bigint"
                         + ",tu tinyint unsigned"
                         + ",su smallint unsigned"
                         + ",iu int unsigned"
                         + ",bu bigint unsigned"
                         + ",ff float "
                         + ",dd double "
                         + ",vrc_tag varchar(200)"
                         + ",ncr_tag nchar(200)"
                        + ")";
            string insertSql = $"insert into ? using {stable} tags(?,?,?,?,?,?,?,?,?,?,?,?,?) values (?,?,?,?,?,?,?,?,?,?,?,?,?,?)";
            Console.WriteLine($"{createTable} {0}", connection.CreateCommand(createTable).ExecuteNonQuery());
            var _insertcmd = connection.CreateCommand(insertSql);
            string subTable = stable + "_s01";
            _insertcmd.Parameters.SubTableName = subTable;
            _insertcmd.Parameters.AddTagsValue(true);// mBinds[0] = TaosMultiBind.MultiBindBool(new bool?[] { true });
            _insertcmd.Parameters.AddTagsValue((sbyte)1);//    mBinds[1] = TaosMultiBind.MultiBindTinyInt(new sbyte?[] { 1 });
            _insertcmd.Parameters.AddTagsValue((short)2);//      mBinds[2] = TaosMultiBind.MultiBindSmallInt(new short?[] { 2 });
            _insertcmd.Parameters.AddTagsValue(3);//     mBinds[3] = TaosMultiBind.MultiBindInt(new int?[] { 3 });
            _insertcmd.Parameters.AddTagsValue(4L);//      mBinds[4] = TaosMultiBind.MultiBindBigint(new long?[] { 4 });
            _insertcmd.Parameters.AddTagsValue((byte)1);//   mBinds[5] = TaosMultiBind.MultiBindUTinyInt(new byte?[] { 1 });
            _insertcmd.Parameters.AddTagsValue((ushort)2);//   mBinds[6] = TaosMultiBind.MultiBindUSmallInt(new ushort?[] { 2 });
            _insertcmd.Parameters.AddTagsValue((uint)3);//  mBinds[7] = TaosMultiBind.MultiBindUInt(new uint?[] { 3 });
            _insertcmd.Parameters.AddTagsValue((ulong)4);// mBinds[8] = TaosMultiBind.MultiBindUBigInt(new ulong?[] { 4 });
            _insertcmd.Parameters.AddTagsValue((float)18.58f);// mBinds[9] = TaosMultiBind.MultiBindFloat(new float?[] { 18.58f });
            _insertcmd.Parameters.AddTagsValue(2020.05071858d);// mBinds[10] = TaosMultiBind.MultiBindDouble(new double?[] { 2020.05071858d });
            _insertcmd.Parameters.AddTagsValue("taosdata");//   mBinds[11] = TaosMultiBind.MultiBindBinary(new string?[] { "taosdata" });
            _insertcmd.Parameters.AddTagsValue("TDenginge".ToCharArray());//    mBinds[12] = TaosMultiBind.MultiBindNchar(new string?[] { "TDenginge" });



            long[] tsArr = new long[5] { 1656677700000, 1656677710000, 1656677720000, 1656677730000, 1656677700000 };
            bool?[] boolArr = new bool?[5] { true, false, null, true, true };
            sbyte?[] tinyIntArr = new sbyte?[5] { -127, 0, null, 8, 127 };
            short?[] shortArr = new short?[5] { short.MinValue + 1, -200, null, 100, short.MaxValue };
            int?[] intArr = new int?[5] { -200, -100, null, 0, 300 };
            long?[] longArr = new long?[5] { long.MinValue + 1, -2000, null, 1000, long.MaxValue };
            float?[] floatArr = new float?[5] { float.MinValue + 1, -12.1F, null, 0F, float.MaxValue };
            double?[] doubleArr = new double?[5] { double.MinValue + 1, -19.112D, null, 0D, double.MaxValue };
            byte?[] uTinyIntArr = new byte?[5] { byte.MinValue, 12, null, 89, byte.MaxValue - 1 };
            ushort?[] uShortArr = new ushort?[5] { ushort.MinValue, 200, null, 400, ushort.MaxValue - 1 };
            uint?[] uIntArr = new uint?[5] { uint.MinValue, 100, null, 2, uint.MaxValue - 1 };
            ulong?[] uLongArr = new ulong?[5] { ulong.MinValue, 2000, null, 1000, long.MaxValue - 1 };
            string?[] binaryArr = new string?[5] { "1234567890~!@#$%^&*()_+=-`[]{}:,./<>?", String.Empty, null, "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM", "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890~!@#$%^&*()_+=-`[]{}:,./<>?" };
            string?[] ncharArr = new string?[5] { "1234567890~!@#$%^&*()_+=-`[]{}:,./<>?", null, "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM", "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890~!@#$%^&*()_+=-`[]{}:,./<>?", string.Empty };



            _insertcmd.Parameters.AddWithValue(DateTime.UnixEpoch.AddMilliseconds(tsArr[0]));// mBinds[0] = TaosMultiBind.MultiBindTimestamp(tsArr);
            _insertcmd.Parameters.AddWithValue(boolArr[0]);// mBinds[1] = TaosMultiBind.MultiBindBool(boolArr);
            _insertcmd.Parameters.AddWithValue(tinyIntArr[0]);//  mBinds[2] = TaosMultiBind.MultiBindTinyInt(tinyIntArr);
            _insertcmd.Parameters.AddWithValue(shortArr[0]);//  mBinds[3] = TaosMultiBind.MultiBindSmallInt(shortArr);
            _insertcmd.Parameters.AddWithValue(intArr[0]);//  mBinds[4] = TaosMultiBind.MultiBindInt(intArr);
            _insertcmd.Parameters.AddWithValue(longArr[0]);// mBinds[5] = TaosMultiBind.MultiBindBigint(longArr);
            _insertcmd.Parameters.AddWithValue(floatArr[0]);// mBinds[6] = TaosMultiBind.MultiBindFloat(floatArr);
            _insertcmd.Parameters.AddWithValue(doubleArr[0]);//mBinds[7] = TaosMultiBind.MultiBindDouble(doubleArr);
            _insertcmd.Parameters.AddWithValue(uTinyIntArr[0]);//      mBinds[8] = TaosMultiBind.MultiBindUTinyInt(uTinyIntArr);
            _insertcmd.Parameters.AddWithValue(uShortArr[0]);//  mBinds[9] = TaosMultiBind.MultiBindUSmallInt(uShortArr);
            _insertcmd.Parameters.AddWithValue(uIntArr[0]);// mBinds[10] = TaosMultiBind.MultiBindUInt(uIntArr);
            _insertcmd.Parameters.AddWithValue(uLongArr[0]);//  mBinds[11] = TaosMultiBind.MultiBindUBigInt(uLongArr);
            _insertcmd.Parameters.AddWithValue(binaryArr[0]);// mBinds[12] = TaosMultiBind.MultiBindBinary(binaryArr);
            _insertcmd.Parameters.AddWithValue(ncharArr[0].ToCharArray());//mBinds[13] = TaosMultiBind.MultiBindNchar(ncharArr);

            Console.WriteLine($"{insertSql}{0}", _insertcmd.ExecuteNonQuery());
            string querySql = $"select * from {stable}";
            var _qreader = connection.CreateCommand(querySql).ExecuteReader();
            ConsoleTableBuilder.From(_qreader.ToDataTable()).WithFormat(ConsoleTableBuilderFormat.Default).ExportAndWriteLine();
        }

        static void UploadTelemetryData(  TaosConnection connection, string devid, string keyname, int count)
        {
            for (int i = 0; i < count; i++)
            {
                connection.CreateCommand($"INSERT INTO device_{devid} USING telemetrydata TAGS(\"{devid}\",\"{keyname}\") values (now,2,true,'{i}',{i},now,{i});").ExecuteNonQuery();
            }
        }

        static void UploadTelemetryDataPool(TaosConnection connection, string devid, string keyname, int count)
        {
            Parallel.For(0, count,new ParallelOptions() { MaxDegreeOfParallelism=connection.PoolSize }, i =>
            {
                try
                {
                    connection.CreateCommand($"INSERT INTO device_{devid} USING telemetrydata TAGS(\"{devid}\",\"{keyname}\") values (now,2,true,'{i}',{i},now,{i});").ExecuteNonQuery();
                    Console.WriteLine($"线程:{Thread.CurrentThread.ManagedThreadId} 第{i}条数据, OK");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"线程:{Thread.CurrentThread.ManagedThreadId} 第{i}条数据, {ex.Message}");
                }
            });
        }
    }
}
