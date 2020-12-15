using Mono.Cecil;
using Swagger4WCF.Constants;
using Swagger4WCF.Data;
using Swagger4WCF.Interfaces;
using Swagger4WCF.YAML.Readers;
using Swagger4WCF.YAML.Writers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Swagger4WCF.YAML
{
    public class Content : IYAMLContent
    {
        public Documentation Documentation { get; }
        private StringBuilder m_Builder = new StringBuilder();
        public Tabulation Tabulation { get; set; } = new Tabulation("  ", 0);
        private Dictionary<string, TypeData> definitionList = new Dictionary<string, TypeData>();
        private HashSet<string> addedTypes = new HashSet<string>();

        static public Document Generate(TypeDefinition type, Documentation documentation, AssemblyDefinition assembly) =>
            new Document(type, new Content(type, documentation, assembly));

        static public implicit operator string(Content compiler) => compiler?.ToString();

        public override string ToString() => this.m_Builder.ToString();

        private Content(TypeDefinition type, Documentation documentation, AssemblyDefinition assembly)
        {
            this.Documentation = documentation;
            this.Add("openapi: '3.0.3'");
            this.Add("info:");
            using (new Block(this))
            {
                this.Add("title: ", type.Name);
                var _customAttribute = type.Module.Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
                var _argument = _customAttribute?.Argument<string>(0);
                if (_argument != null)
                {
                    this.Add($"version: \"{ _argument }\"");
                }
            }
            this.Add("servers:");
            using (new Block(this))
            {
                this.Add($"- url: http://localhost/{type.Name}");
                this.Add($"- url: https://localhost/{type.Name}");
            }
            this.Add("paths:");

            var _allMethods = new List<MethodData>();
            {
                var allTypes = new List<TypeDefinition> { type };
                allTypes.AddRange(type.Interfaces.Select(interf => assembly.MainModule.GetType(interf.InterfaceType.FullName)));
                allTypes.ForEach(currentTypes => _allMethods.AddRange(AddMethodsForType(currentTypes)));
            }
            List<TypeData> definitions = TypesReader.Instance.GetUsedTypes(_allMethods);
            this.WriteDefinitions(definitions);
        }

        private void WriteDefinitions(List<TypeData> definitions)
        {
            if (!definitions.Any())
                return;

            this.Add("components:");
            using (new Block(this))
            {
                this.Add("schemas:");
                using (new Block(this))
                    definitions.ForEach(type => TypeWriter.Instance.Write(type, this));
            }
        }

        private List<MethodData> AddMethodsForType(TypeDefinition type)
        {
            var typeData = new TypeData(type, this.Documentation);
            var methodsGroupedByPath = typeData.Methods.GroupBy(m => m.WebInvoke.UriTemplate, key => key).ToDictionary(k => k.Key, v => v);
            using (new Block(this))
            {
                foreach (var _method in methodsGroupedByPath.Keys)
                {
                    if (string.IsNullOrWhiteSpace(_method))
                        continue;

                    this.Add("/", _method, ":");
                    foreach (var m in methodsGroupedByPath[_method])
                        this.Add(m);
                }
            }

            return typeData.Methods;
        }

        public void Add(TypeReference type)
        {
            type = (type is GenericInstanceType genType) ? genType.GenericArguments[0] : type;
            if (type.Resolve() == type.Module.ImportReference(typeof(string)).Resolve())
            {
                this.Add("type: \"string\"");
            }
            else if (type.Resolve() == type.Module.ImportReference(typeof(bool)).Resolve())
            {
                this.Add("type: \"boolean\"");
            }
            else if (type.Resolve() == type.Module.ImportReference(typeof(int)).Resolve())
            {
                this.Add("type: \"number\"");
                this.Add("format: int32");
            }
            else if (type.Resolve() == type.Module.ImportReference(typeof(short)).Resolve())
            {
                this.Add("type: \"number\"");
                this.Add("format: int16");
            }
            else if (type.Resolve() == type.Module.ImportReference(typeof(byte)).Resolve())
            {
                this.Add("type: \"number\"");
                this.Add("format: int8");
            }
            else if (type.Resolve() == type.Module.ImportReference(typeof(long)).Resolve())
            {
                this.Add("type: \"number\"");
                this.Add("format: int32");
            }
            else if (type.Resolve() == type.Module.ImportReference(typeof(decimal)).Resolve())
            {
                this.Add("type: \"number\"");
                this.Add("format: decimal(9,2)");
            }
            else if (type.Resolve() == type.Module.ImportReference(typeof(DateTime)).Resolve())
            {
                this.Add("type: \"string\"");
                this.Add("format: date-time");
            }
            else if (type.Resolve() == type.Module.ImportReference(typeof(Stream)).Resolve())
            {
                this.Add("type: \"string\"");
                this.Add("format: binary");
            }
            else if (type.IsArray)
            {
                this.Add("type: array");
                this.Add("items:");
                using (new Block(this)) { this.Add(type.GetElementType()); }
            }
            else if (type is TypeDefinition typeDef && typeDef.IsEnum)
            {
                this.Add("type: number");
                this.Add("enum:");

                foreach (var field in typeDef.Fields)
                {
                    if (field.Name == "value__")
                        continue;
                    this.Add($"- {field.Constant}");
                }
            }
            else
            {
                this.Add("$ref: \"#/components/schemas/", type.Name, "\"");
            }
        }

        public void Add(params string[] line) =>
            this.m_Builder.AppendLine(this.Tabulation.ToString() + string.Concat(line));

        private void Add(MethodData method) =>
            MethodWriter.Instance.Write(method, this);
    }
}
