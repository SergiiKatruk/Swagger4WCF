using Mono.Cecil;
using Swagger4WCF.Constants;
using Swagger4WCF.Data;
using Swagger4WCF.Interfaces;
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
			this.Add("components:");
			using (new Block(this))
			{
				this.Add("schemas:");
				AddDefinitionsForMethods(_allMethods);
			}
		}

		private void AddDefinitionsForMethods(List<MethodData> _methods)
		{
			using (new Block(this))
			{
				foreach (var type in _methods.Select(_Method => _Method.ReturnType).Distinct()
					.OrderBy(typeRef => typeRef.Name)
					.Where(typeRef => !typeRef.IsValueType && typeRef.Name != "Stream"))
					definitionList[type.Type.FullName] = type;

				foreach (var type in _methods.SelectMany(_Method => _Method.Parameters).Select(x => x.TypeData)
					.OrderBy(typeRef => typeRef.Name)
					.Where(typeRef => !typeRef.IsValueType && typeRef.Name != "Stream"))
					definitionList[type.Type.FullName] = type;

				while (this.definitionList.Any())
				{
					var currentDef = this.definitionList;
					this.definitionList = new Dictionary<string, TypeData>();
					foreach (var def in currentDef)
					{
						this.addedTypes.Add(def.Key);
						ParseComplexType(def.Value);
					}
				}
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

		public void AddRequestBody(ParameterData parameter, string responseFormat)
		{
			using (new Block(this))
			{
				if (!string.IsNullOrWhiteSpace(parameter.Description))
					this.Add("description: ", parameter.Description);
				this.Add("required: ", parameter.IsRequired.ToString());
				this.Add("content:");
				using (new Block(this))
				{
					this.Add(responseFormat);
					using (new Block(this))
					{
						this.Add("schema:");
						using (new Block(this))
							this.Add(parameter.Type);
					}
				}
			}
		}

		public void Add(TypeReference type)
		{
			if (type.Resolve() == type.Module.ImportReference(typeof(string)).Resolve())
			{
				this.Add("type: \"string\"");
			}
			else if (type.Resolve() == type.Module.ImportReference(typeof(bool)).Resolve()
					 || type.Resolve() == type.Module.ImportReference(typeof(bool?)).Resolve())
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
			else if (type.Resolve() == type.Module.ImportReference(typeof(long)).Resolve())
			{
				this.Add("type: \"number\"");
				this.Add("format: int32");
			}
			else if (type.Resolve() == type.Module.ImportReference(typeof(decimal)).Resolve()
					 || type.Resolve() == type.Module.ImportReference(typeof(decimal?)).Resolve())
			{
				this.Add("type: \"number\"");
				this.Add("format: decimal(9,2)");
			}
			else if (type.Resolve() == type.Module.ImportReference(typeof(DateTime)).Resolve()
					 || type.Resolve() == type.Module.ImportReference(typeof(DateTime?)).Resolve())
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
				if (type.Resolve()?.GetCustomAttribute<DataContractAttribute>() != null)
				{
					if (!this.addedTypes.Contains(type.FullName))
					{
						this.addedTypes.Add(type.FullName);
						definitionList[type.FullName] = new TypeData(type.Resolve(), this.Documentation);
					}
					this.Add("$ref: \"#/components/schemas/", type.Name, "\"");
				}
				else
				{
					this.Add("$ref: \"#/components/schemas/", type.Name, "\"");
				}
			}
		}

		public void Add(params string[] line) =>
			this.m_Builder.AppendLine(this.Tabulation.ToString() + string.Concat(line));

		private void Add(MethodData method) =>
			MethodWriter.Instance.Write(method, this);

		public void Add(ParameterData parameter) =>
			ParameterWriter.Instance.Write(parameter, this);

		private void Add(PropertyData property) =>
			PropertyWriter.Instance.Write(property, this);

		private void ParseComplexType(TypeData type) =>
			TypeWriter.Instance.Write(type, this);
	}
}
