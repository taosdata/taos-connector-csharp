using ConsoleTableExt;
using Maikebing.Data.Taos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace TaosADODemo
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            ///Specify the name of the database
            string database = "db_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var builder = new TaosConnectionStringBuilder()
            {
                DataSource = "114.116.231.247",
                DataBase = database,
                Username = "kangyunqiang",
                Password = "kangyunqiang",
                Port=6060

            };
            //Example for ADO.Net 
            using (var connection = new TaosConnection(builder.ConnectionString))
            {
                connection.Open();
             
                Console.WriteLine("create {0} {1}", database, connection.CreateCommand($"create database {database};").ExecuteNonQuery());
                Console.WriteLine("create table t {0} {1}", database, connection.CreateCommand($"create table {database}.t (ts timestamp, cdata int);").ExecuteNonQuery());
                Console.WriteLine("insert into t values  {0}  ", connection.CreateCommand($"insert into {database}.t values ({(long)(DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds)}, 10);").ExecuteNonQuery());
                //Console.WriteLine("insert into t values  {0} ", connection.CreateCommand($"insert into {database}.t values ({(long)(DateTime.Now.AddMonths(1).Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds)}, 20);").ExecuteNonQuery());
                var cmd_select = connection.CreateCommand();
                cmd_select.CommandText = $"select * from {database}.t";
                var reader = cmd_select.ExecuteReader();
                Console.WriteLine(cmd_select.CommandText);
                Console.WriteLine("");
                ConsoleTableBuilder.From(reader.ToDataTable()).WithFormat(ConsoleTableBuilderFormat.MarkDown).ExportAndWriteLine();
                Console.WriteLine("");
                Console.WriteLine("DROP TABLE  {0} {1}", database, connection.CreateCommand($"DROP TABLE  {database}.t;").ExecuteNonQuery());
                Console.WriteLine("DROP DATABASE {0} {1}", database, connection.CreateCommand($"DROP DATABASE   {database};").ExecuteNonQuery());
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
                    context.sensor.Add(new sensor() { ts = DateTime.Now.AddMilliseconds(i), degree = rd.NextDouble(), pm25 = rd.Next(0, 1000) });
                }
                Console.WriteLine("Saveing");
                context.SaveChanges();
                Console.WriteLine("");
                Console.WriteLine("from s in context.sensor where s.pm25 > 0 select s ");
                Console.WriteLine("");
                var f = from s in context.sensor where s.pm25 > 0 select s;
                var ary = f.ToArray();
                ConsoleTableBuilder.From(ary.ToList()).WithFormat(ConsoleTableBuilderFormat.MarkDown).ExportAndWriteLine();
                context.Database.EnsureDeleted();
            }
            Console.WriteLine("");
        }
    }
}
