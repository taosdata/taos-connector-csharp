# Maikebing.EntityFrameworkCore.Taos

## 项目简介
 

Entity, Framework, EF, Core, Data, O/RM, entity-framework-core,TDengine
--

Maikebing.Data.Taos  是一个基于TDengine 的RESTful Connector构建的ADO.Net 提供器。 这将允许你通过.Net Core 访问TDengine数据库。

---

Maikebing.EntityFrameworkCore.Taos 是一个Entity Framework Core 的提供器， 基于Maikebing.Data.Taos构建。 

## 关于RESTful Connector

由于官方没有提供任何已编译可供Linux、Windows、MacOS的C++ Connector,要实现.Net Core 横跨三平台需要研读C++ Connector代码并在三个平台下进行测试。
与此同时又需要阅读ADO.Net 和EFCore 框架的代码， 压力巨大，因此我先使用 RESTful Connector 等官方发布三平台下经过测试的动态库后再重写ADO.Net。 


---

[![Build status](https://ci.appveyor.com/api/projects/status/8krjmvsoiilo2r10?svg=true)](https://ci.appveyor.com/project/MaiKeBing/maikebing-entityframeworkcore-taos)
[![License](https://img.shields.io/github/license/maikebing/Maikebing.EntityFrameworkCore.Taos.svg)](https://github.com/maikebing/Maikebing.EntityFrameworkCore.Taos/blob/master/LICENSE)
![Nuget](https://img.shields.io/nuget/v/Maikebing.Data.Taos.svg)

---

##  How to install?
 ` Install-Package Maikebing.Data.Taos`

## How to use?

```C#
///Specify the name of the database
string database = "db_" + DateTime.Now.ToString("yyyyMMddHHmmss");
var builder = new TaosConnectionStringBuilder()
{
    DataSource = "http://td.gitclub.cn/rest/sql",
    DataBase = database,
    Username = "root",
    Password = "taosdata"
};
//Example for ADO.Net 
using (var connection = new TaosConnection(builder.ConnectionString))
{
    connection.Open();
    Console.WriteLine("create {0} {1}", database, connection.CreateCommand($"create database {database};").ExecuteNonQuery());
    Console.WriteLine("create table t {0} {1}", database, connection.CreateCommand($"create table {database}.t (ts timestamp, cdata int);").ExecuteNonQuery());
    Console.WriteLine("insert into t values  {0}  ", connection.CreateCommand($"insert into {database}.t values ('{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ms")}', 10);").ExecuteNonQuery());
    Console.WriteLine("insert into t values  {0} ", connection.CreateCommand($"insert into {database}.t values ('{DateTime.Now.AddMonths(1).ToString("yyyy-MM-dd HH:mm:ss.ms")}', 20);").ExecuteNonQuery());
    Console.WriteLine("==================================================================");
    var cmd_select = connection.CreateCommand();
    cmd_select.CommandText = $"select * from {database}.t";
    var reader = cmd_select.ExecuteReader();
    ConsoleTableBuilder.From(reader.ToDataTable()).WithFormat(ConsoleTableBuilderFormat.MarkDown).ExportAndWriteLine();
    Console.WriteLine("==================================================================");
    Console.WriteLine("DROP TABLE  {0} {1}", database, connection.CreateCommand($"DROP TABLE  {database}.t;").ExecuteNonQuery());
    Console.WriteLine("DROP DATABASE {0} {1}", database, connection.CreateCommand($"DROP DATABASE   {database}").ExecuteNonQuery());
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
    Console.WriteLine("SaveChanges.....");
    context.SaveChanges();
    Console.WriteLine("==================================================================");
    Console.WriteLine("Search   pm25>0");
    var f = from s in context.sensor where s.pm25 > 0 select s;
    var ary = f.ToArray();
    ConsoleTableBuilder.From(ary.ToList()).WithFormat(ConsoleTableBuilderFormat.MarkDown).ExportAndWriteLine();
    Console.WriteLine("==================================================================");
    context.Database.EnsureDeleted();
}
}
```