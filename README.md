# IoTSharp.EntityFrameworkCore.Taos

## 项目简介


Entity, Framework, EF, Core, Data, O/RM, entity-framework-core,TDengine
--

IoTSharp.Data.Taos  是一个采用TDengine的原生动态库构建的ADO.Net提供程序。 它将允许你通过.Net Core 访问TDengine 数据库。

---

IoTSharp.EntityFrameworkCore.Taos 是一个Entity Framework Core 的提供器， 基于IoTSharp.Data.Taos实现。 
(原名称为 Maikebing.EntityFrameworkCore.Taos)

---

[![Build status](https://ci.appveyor.com/api/projects/status/8krjmvsoiilo2r10?svg=true)](https://ci.appveyor.com/project/iotsharp/entityframeworkcore-taos)
[![License](https://img.shields.io/github/license/iotsharp/EntityFrameworkCore.Taos.svg)](https://github.com/IoTSharp/EntityFrameworkCore.Taos/blob/master/LICENSE)



| NuGet名称    | 版本|下载量| 说明                                                     |
| ----------- | --------  | --------  | ------------------------------------------------------------ |
| IoTSharp.Data.Taos |[![IoTSharp.Data.Taos](https://img.shields.io/nuget/v/iotsharp.Data.Taos.svg)](https://www.nuget.org/packages/IoTSharp.Data.Taos/) |![Nuget](https://img.shields.io/nuget/dt/IoTSharp.Data.Taos) |ADO.Net Core 基础组件
| IoTSharp.EntityFrameworkCore.Taos |[![IoTSharp.EntityFrameworkCore.Taos](https://img.shields.io/nuget/v/IoTSharp.EntityFrameworkCore.Taos.svg)](https://www.nuget.org/packages/IoTSharp.EntityFrameworkCore.Taos/) |![Nuget](https://img.shields.io/nuget/dt/IoTSharp.EntityFrameworkCore.Taos)     | 供EF Core使用的组件
| IoTSharp.HealthChecks.Taos |[![IoTSharp.HealthChecks.Taos](https://img.shields.io/nuget/v/IoTSharp.HealthChecks.Taos.svg)](https://www.nuget.org/packages/IoTSharp.HealthChecks.Taos/)  |  ![Nuget](https://img.shields.io/nuget/dt/IoTSharp.HealthChecks.Taos)| 供Asp.Net Core 使用的健康检查组件


---


[TDengine技术开放日 — 从技术创新和设计思想，认识TDengine](https://live.photoplus.cn/live/pc/38916035/#/live)

![荣誉证书](docs/taos_ch_600.png)




##  如何使用？

 例子:

![Example](docs/Example.png)

```csharp
    ///Specify the name of the database
    string database = "db_" + DateTime.Now.ToString("yyyyMMddHHmmss");
      string database = "db_" + DateTime.Now.ToString("yyyyMMddHHmmss");
      var builder = new TaosConnectionStringBuilder()
      {
            DataSource = "127.0.0.1",
            DataBase = database,
            Username = "root",
            Password = "kissme",
            Port=6060
            };
    //Example for ADO.Net 
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
    Console.WriteLine("Pass any key to exit....");
    Console.ReadKey();
```


用于物联网的超级表示例:

[IoTSharp/Storage/TaosStorage.cs](https://github.com/IoTSharp/IoTSharp/blob/master/IoTSharp/Storage/TaosStorage.cs)

```

   using (var connection = new TaosConnection(builder.ConnectionString))
            {
                connection.Open();
                Console.WriteLine("ServerVersion:{0}", connection.ServerVersion);
                connection.CreateCommand("DROP DATABASE IF EXISTS  IoTSharp").ExecuteNonQuery();
                connection.CreateCommand("CREATE DATABASE IoTSharp KEEP 365 DAYS 10 BLOCKS 4;").ExecuteNonQuery();
                connection.ChangeDatabase("IoTSharp");
                connection.CreateCommand("CREATE TABLE IF NOT EXISTS telemetrydata  (ts timestamp,value_type  tinyint, value_boolean bool, value_string binary(10240), value_long bigint,value_datetime timestamp,value_double double)   TAGS (deviceid binary(32),keyname binary(64));").ExecuteNonQuery();
                //connection.CreateCommand($"CREATE TABLE dev_Thermometer USING telemetrydata TAGS (\"Temperature\")").ExecuteNonQuery();
                var devid = $"{Guid.NewGuid():N}";
                UploadTelemetryData(connection, devid, "Temperature", 999);
                UploadTelemetryData(connection,devid,   "Humidity", 888);
                var devid2 = $"{Guid.NewGuid():N}";
                UploadTelemetryData(connection, devid2, "Temperature", 777);
                UploadTelemetryData(connection, devid2, "Humidity", 666);
                var reader2 = connection.CreateCommand("select last_row(*) from telemetrydata group by deviceid,keyname ;").ExecuteReader();
                ConsoleTableBuilder.From(reader2.ToDataTable()).WithFormat(ConsoleTableBuilderFormat.Default).ExportAndWriteLine();
                connection.Close();
            }
            
             static void UploadTelemetryData(  TaosConnection connection, string devid, string keyname, int count)
        {
            for (int i = 0; i < count; i++)
            {
                connection.CreateCommand($"INSERT INTO device_{devid}_{keyname} USING telemetrydata TAGS(\"{devid}\",\"{keyname}\")  (ts,value_type,value_long) values (now,2,{i});").ExecuteNonQuery();
            }
        }
        
```
