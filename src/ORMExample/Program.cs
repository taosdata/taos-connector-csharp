using IoTSharp.Data.Taos;
using IoTSharp.ORM.Taos;
using System;
using System.ComponentModel;
using System.Data.Common;

namespace ORMExample
{

    [Taos("BREAKER_BASIC")]//超级表名
    [Description("时序表")]
    public class EntityDemo
    {
        /// <summary>
        /// 特性含义:别名,类型，类型长度，是否为Tag,是否为表名[表名只能设置一个字段]
        /// </summary>
        [TaosColumn("equipId", TaosDataType.BINARY, "64", true, true)]
        public string EquipId { get; set; }

        [TaosColumn("groupid", TaosDataType.INT, isTag: true)]
        public int GroupId { get; set; }

        [TaosColumn("current", TaosDataType.FLOAT)]
        public double Current { get; set; }

        [TaosColumn("voltage", TaosDataType.INT)]
        public int Voltage { get; set; }

        [TaosColumn("phase", TaosDataType.FLOAT)]
        public double Phase { get; set; }
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            DbProviderFactories.RegisterFactory("TDengine", TaosFactory.Instance);
            ///Specify the name of the database
            string database = "db_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var builder = new TaosConnectionStringBuilder()
            {
                DataSource = "airleaderserver",
                DataBase = database,
                Username = "root",
                Password = "taosdata",
                Port = 6030

            };
            TaosOrm taosSugar = new TaosOrm(builder.ConnectionString);
            _ = taosSugar.TaosAddAsync(
                new EntityDemo()
                {
                    Current = 2,
                    Phase = 30,
                    Voltage = 4,
                    EquipId = "3afbe36dd5b740a6b7c1634b70687d51",
                    GroupId = 1
                }, true);
            Console.WriteLine("Hello World!");
        }
    }
}
