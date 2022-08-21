using CommandLine;
using CppSharp;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System.Text;

namespace ConsoleAppCppSharp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<Options>(args)
                    .WithParsedAsync(async o =>
                    {

                        var client = new HttpClient();
                        using var buffer = await client.GetStreamAsync($"https://www.taosdata.com/assets-download/3.0/TDengine-client-{o.Version}-Linux-x64.tar.gz");
                        if (!Directory.Exists(o.Path)) Directory.CreateDirectory(o.Path);
                       ExtractTarGz(buffer, o.Path);
                        ExtractTarGz(Path.Combine(o.Path, $"TDengine-client-{o.Version}", "taos.tar.gz"), Path.Combine(o.Path, "taos"));
                  
                        ConsoleDriver.Run(new TaosLibrary(o.Path, o.Version));
                    });
        }



        public static bool ExtractTarGz(string path, string goalFolder)
        {
            using (var inStream = File.OpenRead(path))
            {
               return  ExtractTarGz(inStream, goalFolder);
            }
        }
        public static bool ExtractTarGz(Stream inStream, string goalFolder)
        {
            using (var gzipStream = new GZipInputStream(inStream))
            {
                var tarArchive = TarArchive.CreateInputTarArchive(gzipStream, Encoding.Default);
                tarArchive.ExtractContents(goalFolder);
                tarArchive.Close();
            }
            return true;
        }
    }
}