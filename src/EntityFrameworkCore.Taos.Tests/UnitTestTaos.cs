using Ductus.FluentDocker.Model.Containers;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Services.Extensions;
using IoTSharp.Data.Taos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data.Common;
using System.Linq;

namespace EntityFrameworkCore.Taos.Tests
{
    [TestClass]
    public class UnitTestTaos
    {
        private IContainerService container;
        private string database;
        private TaosConnectionStringBuilder builder;

        [TestInitialize]
        public void  Initialize()
        {
            database = "db_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            DbProviderFactories.RegisterFactory("TDengine", TaosFactory.Instance);
            var hosts = new Hosts().Discover();
            var _docker = hosts.FirstOrDefault(x => x.IsNative) ?? hosts.FirstOrDefault(x => x.Name == "default");
            container = _docker.Create("tdengine/tdengine:2.4.0.12", true, new ContainerCreateParams
            {
                PortMappings = new[] { "6030:6030", "6035:6035", "6030:6030/udp", "6035:6035/udp" },
                Hostname = System.Net.Dns.GetHostName()
            });
            container.Start();
            container.WaitForPort("6030/tcp");
            builder = new TaosConnectionStringBuilder()
            {
                DataSource = System.Net.Dns.GetHostName(),
                DataBase = database,
                Username = "root",
                Password = "taosdata",
                Port = 6030
            };
        }

        [TestCleanup]
        public void Clianup()
        {
            container.Stop();
            container.Dispose();
        }


        [TestMethod]
        public void TestExecuteBulkInsert_Lines()
        {
            using (var connection = new TaosConnection(builder.ConnectionString))
            {
                connection.Open();
                string[] lines = {
                    "meters,location=Beijing.Haidian,groupid=2 current=11.8,voltage=221,phase=0.28 1648432611249",
                    "meters,location=Beijing.Haidian,groupid=2 current=13.4,voltage=223,phase=0.29 1648432611250",
                    "meters,location=Beijing.Haidian,groupid=3 current=10.8,voltage=223,phase=0.29 1648432611249",
                    "meters,location=Beijing.Haidian,groupid=3 current=11.3,voltage=221,phase=0.35 1648432611250"
                };
                int result = connection.ExecuteBulkInsert(lines);
                Assert.AreEqual(lines.Length, result);
                connection.Close();
            }
        }
    }
}