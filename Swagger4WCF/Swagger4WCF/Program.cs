using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Swagger4WCF
{
    static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length==0)
            {
                args = new string[5];
                Console.WriteLine("Please provide path to sln");
                args[0]= Console.ReadLine();
                Console.WriteLine("Please provide path to project file");
                args[1] = Console.ReadLine();
                Console.WriteLine("Please provide configuration Release or Debug");
                args[2] = Console.ReadLine();
                Console.WriteLine("Please provide Solution Platform (Any CPU)");
                args[3] = Console.ReadLine();
                Console.WriteLine("Please provide path to dll");
                args[4] = Console.ReadLine();
            }

            try
            {
                ExecSwaggerWCF(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void ExecSwaggerWCF(string[] args)
        {
            var _directory = Path.GetDirectoryName(args[4]);
            var _name = Path.GetFileNameWithoutExtension(args[1]);
            string interfaceToGenerateName = GetStringArgument(args, 5);
            string dllFileName = GetStringArgument(args, 4);
            var _resolver = new DefaultAssemblyResolver();
            _resolver.AddSearchDirectory(_directory);
            var dllsToParse = new List<string>();
            if (dllFileName == null || !dllFileName.EndsWith(".dll"))
                dllsToParse.AddRange(Directory.EnumerateFiles(_directory, "*.dll"));
            else
                dllsToParse.Add(dllFileName);
            var _domain = dllsToParse.Select(_File =>
            {
                try
                {
                    return new
                    {
                        Assembly = AssemblyDefinition.ReadAssembly(_File,
                            new ReaderParameters()
                            {
                                AssemblyResolver = _resolver,
                                ReadSymbols = true,
                                ReadingMode = ReadingMode.Immediate
                            }),
                        Location = _File
                    };
                }
                catch { return null; }
            }).Where(_Entry => _Entry != null).ToArray();
            foreach (var _entry in _domain)
            {
                foreach (var _document in YAML.Generate(_entry.Assembly, Documentation.Load(_entry.Location, _entry.Assembly), interfaceToGenerateName))
                {
                    var _location = $@"{ _directory }\{ _entry.Assembly.Name.Name }.{_document.Type.Name }.yaml";
                    using (var _writer = new StreamWriter(_location, false, Encoding.UTF8))
                    {
                        _writer.Write(_document.ToString());
                        Console.WriteLine($"{ _name } -> { _location }");
                    }
                }
            }
        }

		private static string GetStringArgument(string[] args, int index) => 
            (index < 0 || args.Length == 0 || args.Length < index) ? null : args[index].ToString();
	}
}
