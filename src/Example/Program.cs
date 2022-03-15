using ConsoleTableExt;
using IoTSharp.Data.Taos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;

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
                Console.WriteLine("create {0} {1}", database, connection.CreateCommand($"create database {database};").ExecuteNonQuery());
                Console.WriteLine("create table t {0} {1}", database, connection.CreateCommand($"create table {database}.t (ts timestamp, cdata int);").ExecuteNonQuery());
                Console.WriteLine("insert into t values  {0}  ", connection.CreateCommand($"insert into {database}.t values ({(long)(DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds)}, 10);").ExecuteNonQuery());
                var pmcmd = connection.CreateCommand($"insert into {database}.t values (@ts, @v);");
                Thread.Sleep(TimeSpan.FromSeconds(1));
                pmcmd.Parameters.AddWithValue("@ts", DateTime.Now);//(long)(DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds));
                pmcmd.Parameters.AddWithValue("@v",1111);
                pmcmd.ExecuteNonQuery();
                Console.WriteLine("单表插入一行数据  {0}  ", connection.CreateCommand($"insert into {database}.t values ({(long)(DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds)}, 10);").ExecuteNonQuery());
                Thread.Sleep(100);
                Console.WriteLine("单表插入多行数据 {0}  ", connection.CreateCommand($"insert into {database}.t values ({(long)(DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds)}, 101) ({(long)(DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds+1)}, 992);").ExecuteNonQuery());

                //Console.WriteLine("insert into t values  {0} ", connection.CreateCommand($"insert into {database}.t values ({(long)(DateTime.Now.AddMonths(1).Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds)}, 20);").ExecuteNonQuery());
                var cmd_select = connection.CreateCommand();
                cmd_select.CommandText = $"select * from {database}.t;";
                var reader = cmd_select.ExecuteReader();

                int index =reader.GetOrdinal("cdata");
                if (reader.Read())
                {
                    var ts = reader.GetDateTime("ts");
                }
                Console.WriteLine($"cdata index at {index}");
                Console.WriteLine(cmd_select.CommandText);
                Console.WriteLine("");
                ConsoleTableBuilder.From(reader.ToDataTable()).WithFormat(ConsoleTableBuilderFormat.MarkDown).ExportAndWriteLine();


                Console.WriteLine("");
                connection.CreateCommand($"CREATE TABLE datas ('reportTime' timestamp, type int, 'bufferedEnd' bool, address nchar(64), parameter nchar(64), value nchar(64)) TAGS ('boxCode' nchar(64), 'machineId' int);").ExecuteNonQuery();
                connection.CreateCommand($"INSERT INTO  data_history_67 USING datas TAGS (mongo, 67) values ( 1608173534840 2 false 'Channel1.窑.烟囱温度' '烟囱温度' '122.00' );").ExecuteNonQuery();
                var cmd_datas = connection.CreateCommand();
                cmd_datas.CommandText = $"SELECT  reportTime,type,bufferedEnd,address,parameter,value FROM  {database}.data_history_67 LIMIT  100";
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
                connection.CreateCommand("CREATE DATABASE IoTSharp KEEP 365 DAYS 10 BLOCKS 4;").ExecuteNonQuery();
                connection.ChangeDatabase("IoTSharp");
                connection.CreateCommand("CREATE STABLE IF NOT EXISTS telemetrydata  (ts timestamp,value_type  tinyint, value_boolean bool, value_string binary(10240), value_long bigint,value_datetime timestamp,value_double double)   TAGS (deviceid binary(32),keyname binary(64));").ExecuteNonQuery();
                //connection.CreateCommand($"CREATE TABLE dev_Thermometer USING telemetrydata TAGS (\"Temperature\")").ExecuteNonQuery();
                var devid = $"{Guid.NewGuid():N}";
                UploadTelemetryData(connection, devid, "Temperature", 999);
                UploadTelemetryData(connection,devid,   "Humidity", 888);
                var devid2 = $"{Guid.NewGuid():N}";
                UploadTelemetryData(connection, devid2, "Temperature", 777);
                UploadTelemetryData(connection, devid2, "Humidity", 666);
                var reader2 = connection.CreateCommand("select last_row(*) from telemetrydata group by deviceid,keyname ;").ExecuteReader();
                ConsoleTableBuilder.From(reader2.ToDataTable()).WithFormat(ConsoleTableBuilderFormat.Default).ExportAndWriteLine();

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
                connection.CreateCommand($"INSERT INTO device_{devid}_{keyname} USING telemetrydata TAGS(\"{devid}\",\"{keyname}\") values (now,2,true,'{i}',{i},now,{i});").ExecuteNonQuery();
            }
        }
    }
}
