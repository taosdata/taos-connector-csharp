using Ductus.FluentDocker.Model.Containers;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Services.Extensions;
using IoTSharp.Data.Taos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

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
                PortMappings = new[] { "6030:6030", "6035:6035", "6041:6041", "6030:6030/udp", "6035:6035/udp" },
                Hostname = System.Net.Dns.GetHostName()
            });
            container.Start();
            var f = container.GetRunningProcesses().Rows.ToList();
            container.WaitForProcess("taosd -c /tmp/taos");
            container.WaitForProcess("taosadapter");
            container.WaitForHttp($"http://{System.Net.Dns.GetHostName()}:6041/rest/login/root/taosdata",(int)TimeSpan.FromSeconds(60).TotalMilliseconds,(Ductus.FluentDocker.Common.RequestResponse response, int stat)=>
            {
                var jo= JObject.Parse(response.Body);
                int result = stat;
                if (  jo.TryGetValue("code",out  JToken?  code))
                {
                    result = (int)(code?.Value<int>());
                }
                return result;
            });
            builder = new TaosConnectionStringBuilder()
            {
                DataSource = System.Net.Dns.GetHostName(),
                DataBase = database,
                Username = "root",
                Password = "taosdata",
                Port = 6030
            };
            NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), DllImportResolver);
        }
        private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName == "taos")
            {
                // On systems with AVX2 support, load a different library.
                if (Environment.OSVersion.Platform== PlatformID.Win32NT  && Environment.Is64BitProcess)
                {
                    return NativeLibrary.Load("taos_win_x64.dll");
                }
                else if (Environment.OSVersion.Platform == PlatformID.Win32NT && !Environment.Is64BitProcess)
                {
                    return NativeLibrary.Load("taos_win_x86.dll");
                }
                else if (Environment.OSVersion.Platform == PlatformID.Unix && Environment.Is64BitProcess)
                {
                    return NativeLibrary.Load("libtaos_linux_x64.so");
                }
            }

            // Otherwise, fallback to default import resolver.
            return IntPtr.Zero;
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
                connection.CreateCommand($"create database {database};").ExecuteNonQuery();
                connection.ChangeDatabase(database);
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