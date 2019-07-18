using Maikebing.Data.Taos;
using Microsoft.EntityFrameworkCore;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TaosADODemo
{
    class Program
    {
        static void Main(string[] args)
        {
            string database = "db_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var builder = new TaosConnectionStringBuilder()
            {
                DataSource = "http://td.gitclub.cn/rest/sql",
                DataBase = database,
               // Token = "cm9vdDp0YW9zZGF0YQ=="
                Username="root",
                 Password= "taosdata"
            };
            using (var connection = new TaosConnection(builder.ConnectionString))
            {
                connection.Open();
                Console.WriteLine("create {0} {1}", database, connection.CreateCommand($"create database {database};").ExecuteNonQuery());

                Console.WriteLine("create table t {0} {1}", database, connection.CreateCommand($"create table {database}.t (ts timestamp, cdata int);").ExecuteNonQuery());

                Console.WriteLine("insert into t values  {0}  ", connection.CreateCommand($"insert into {database}.t values ('{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ms")}', 10);").ExecuteNonQuery());

                Console.WriteLine("insert into t values  {0} ", connection.CreateCommand($"insert into {database}.t values ('{DateTime.Now.AddMonths(1).ToString("yyyy-MM-dd HH:mm:ss.ms")}', 20);").ExecuteNonQuery());
                 
                var cmd_select = connection.CreateCommand();
                cmd_select.CommandText = $"select * from {database}.t";
                var reader = cmd_select.ExecuteReader();
                List<Dictionary<string, object>> valuePairs = new List<Dictionary<string, object>>();
                while (reader.Read())
                {
                    Dictionary<string, object> pairs = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        pairs.Add(reader.GetName(i), reader.GetValue(i));
                    }
                    valuePairs.Add(pairs);
                }
                Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(valuePairs));

                Console.WriteLine("DROP TABLE  {0} {1}", database, connection.CreateCommand($"DROP TABLE  {database}.t;").ExecuteNonQuery());

                Console.WriteLine("DROP DATABASE {0} {1}", database, connection.CreateCommand($"DROP DATABASE   {database}").ExecuteNonQuery());

              
                connection.Close();
            }

            using (var context = new TaosContext())
            {
                context.Database.EnsureCreated();
                context.sensor.Add(new sensor() { ts = DateTime.Now, degree = 1.222, pm25 = 222 });
                Console.ReadKey();
                context.sensor.Add(new sensor() { ts = DateTime.Now, degree = 222, pm25 = 1 });
                Console.ReadKey();
                context.sensor.Add(new sensor() { ts = DateTime.Now, degree = 1.222, pm25 = 222 });
                Console.ReadKey();
                context.sensor.Add(new sensor() { ts = DateTime.Now, degree = 1.222, pm25 = 1 });
                context.SaveChanges();
                var f = from s in context.sensor where s.pm25 == 1 select s;
                var ary = f.ToArray();
                foreach (var x in   ary)
                {
                    Console.WriteLine($"{ x.ts } { x.degree }  {x.pm25}");
                    Console.WriteLine();
                }
            }
            Console.ReadKey();
        }
    }
}
