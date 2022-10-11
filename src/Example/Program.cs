using ConsoleTableExt;
using IoTSharp.Data.Taos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TaosADODemo
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            DbProviderFactories.RegisterFactory("TDengine",  TaosFactory .Instance);
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
                string tableName = "ntb_stmt_cases_test_bind_single_line_cn";
                string createTb = $"create table if not exists {tableName} (" +
                                    "ts timestamp," +
                                    "tt tinyint," +
                                    "si smallint," +
                                    "ii int," +
                                    "bi bigint," +
                                    "tu tinyint unsigned," +
                                    "su smallint unsigned," +
                                    "iu int unsigned," +
                                    "bu bigint unsigned," +
                                    "ff float," +
                                    "dd double," +
                                    "bb binary(200)," +
                                    "nc nchar(200)," +
                                    "bo bool" +
                                    ");";
                string insertSql = $"insert into {tableName} values(?,?,?,?,?,?,?,?,?,?,?,?,?,?);";
              //  string insertSql = "insert into ? values(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)";
                string dropSql = $"drop table if exists {tableName}";
                string querySql = "select * from " + tableName;
                Console.WriteLine($"{dropSql} {0}", connection.CreateCommand(dropSql).ExecuteNonQuery());
                Console.WriteLine($"{createTb} {0}",  connection.CreateCommand(createTb).ExecuteNonQuery());
                var _insertcmd= connection.CreateCommand(insertSql);
               // _insertcmd.Parameters.AddWithValue("@",tableName);  
                _insertcmd.Parameters.AddWithValue(DateTime.Now);// TaosBind.BindTimestamp(1637064040000);
                _insertcmd.Parameters.AddWithValue((sbyte)-2);//TaosBind.BindTinyInt(-2);
                _insertcmd.Parameters.AddWithValue(short.MaxValue);// TaosBind.BindSmallInt(short.MaxValue);
                _insertcmd.Parameters.AddWithValue(int.MaxValue);//TaosBind.BindInt(int.MaxValue);
                _insertcmd.Parameters.AddWithValue(Int64.MaxValue);//TaosBind.BindBigInt(Int64.MaxValue);
                _insertcmd.Parameters.AddWithValue((byte)(byte.MaxValue - 1));//= TaosBind.BindUTinyInt(byte.MaxValue - 1);
                _insertcmd.Parameters.AddWithValue((ushort)(UInt16.MaxValue - 1));//TaosBind.BindUSmallInt(UInt16.MaxValue - 1);
                _insertcmd.Parameters.AddWithValue((uint)(uint.MinValue + 1));//TaosBind.BindUInt(uint.MinValue + 1);
                _insertcmd.Parameters.AddWithValue((ulong)(UInt64.MinValue + 1));//TaosBind.BindUBigInt(UInt64.MinValue + 1);
                _insertcmd.Parameters.AddWithValue(11.11F);//TaosBind.BindFloat(11.11F);
                _insertcmd.Parameters.AddWithValue(22.22D);// TaosBind.BindDouble(22.22D);
                _insertcmd.Parameters.AddWithValue("TDengine数据");// TaosBind.BindBinary("TDengine数据");
                _insertcmd.Parameters.AddWithValue("taosdata涛思数据");//TaosBind.BindNchar("taosdata涛思数据");
                _insertcmd.Parameters.AddWithValue(true);//TaosBind.BindBool(true);
              //  _insertcmd.Parameters.AddWithValue(DBNull.Value);//TaosBind.BindNil();
                 Console.WriteLine($"{insertSql}{0}",    _insertcmd.ExecuteNonQuery());

                var _qreader = connection.CreateCommand(querySql).ExecuteReader();
                ConsoleTableBuilder.From(_qreader.ToDataTable()).WithFormat(ConsoleTableBuilderFormat.Default).ExportAndWriteLine();

                //Console.WriteLine("insert into t values  {0} ", connection.CreateCommand($"insert into {database}.t values ({(long)(DateTime.Now.AddMonths(1).Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds)}, 20);").ExecuteNonQuery());
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

                string[] lines = {
                    "meters,location=Beijing.Haidian,groupid=2 current=11.8,voltage=221,phase=0.28 1648432611249",
                    "meters,location=Beijing.Haidian,groupid=2 current=13.4,voltage=223,phase=0.29 1648432611250",
                    "meters,location=Beijing.Haidian,groupid=3 current=10.8,voltage=223,phase=0.29 1648432611249",
                    "meters,location=Beijing.Haidian,groupid=3 current=11.3,voltage=221,phase=0.35 1648432611250"
                };
                int result = connection.ExecuteBulkInsert(lines);
                Console.WriteLine($"行插入{ result}");
                if (result != lines.Length)
                {
                    throw new Exception("ExecuteBulkInsert");
                }

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
                Console.WriteLine($"Json插入{ result}");
                if (result != lines.Length)
                {
                    throw new Exception("ExecuteBulkInsert");
                }


                var jo = new JObject();
                jo.Add("metric", "stb0_0");
                jo.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());// 1626006833610);
                jo.Add("value", 10);
                var tags1 = new JObject();
                tags1.Add("t1", true);
                tags1.Add("t2", false);
                tags1.Add("t3", 10);
                tags1.Add("t4", "123_abc_.!@#$%^&*:;,./?|+-=()[]{}<>");
                jo.Add("tags", tags1);
                int resultjson2 = connection.ExecuteBulkInsert(new JObject[] { jo },  TDengineDriver.TDengineSchemalessPrecision.TSDB_SML_TIMESTAMP_NOT_CONFIGURED );
                Console.WriteLine($"行插入{ result}");
                if (result != lines.Length)
                {
                    throw new Exception("ExecuteBulkInsert");
                }

                static JObject AddTag(JObject tags, string name, object value, string type)
                {
                    var tag = new JObject();
                    tag.Add("value", true);
                    tag.Add("type", "bool");
                    tags.Add(name, tag);
                    return tag;
                }

                var payload = new JObject();
                var tags = new JObject();
                payload.Add("metric", "stb3_0");

                var timestamp = new JObject();
                timestamp.Add("value",  DateTimeOffset.Now.ToUnixTimeSeconds() );
                timestamp.Add("type", "s");
                payload.Add("timestamp", timestamp);

                var metric_val = new JObject();
                metric_val.Add("value", "hello");
                metric_val.Add("type", "nchar");
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
                Console.WriteLine($"行插入{ result}");
                if (resultjson3 != 1)
                {
                    throw new Exception("ExecuteBulkInsert");
                }




                Console.WriteLine("DROP DATABASE IoTSharp", database, connection.CreateCommand($"DROP DATABASE IoTSharp;").ExecuteNonQuery());

           

                connection.Close();

       

        }
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
