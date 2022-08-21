using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppCppSharp
{
    internal class TaosLibrary : ILibrary
    {
        private string path;
        private readonly string version;

        public TaosLibrary(string path,string version)
        {
            this.path = path;
            this.version = version;
        }
 

        public void Setup(CppSharp.Driver driver)
        {
            var options = driver.Options;
            options.GeneratorKind = GeneratorKind.CSharp;
            var module = options.AddModule("taos");
            module.IncludeDirs.Add(System.IO.Path.Combine(path,  "taos","inc"));
            module.Headers.Add("taos.h");
       
            // module.LibraryDirs.Add(System.IO.Path.Combine(path, $"TDengine-client-{version}", "driver"));
            //module.Libraries.Add($"libtaos.so.{version}");
            options.OutputDir = path;
            module.OutputNamespace = "taos";
        }

        public void Preprocess(Driver driver, ASTContext ctx)
        {
        }

        public void Postprocess(Driver driver, ASTContext ctx)
        {
        }

        public void SetupPasses(Driver driver)
        {
            driver.Generator.Context.ParserOptions.LanguageVersion =
      CppSharp.Parser.LanguageVersion.C99;
        }
    }
}
