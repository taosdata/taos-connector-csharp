using CommandLine.Text;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppCppSharp
{
    public class Options
    {


        [Option(
         Required = true,
          HelpText = "输入解压路径.")]
        public string Path { get; set; }

        [Option(
                Required = true,
          HelpText = "涛思数据的版本")]
        public string Version { get; set; }


    }

}
