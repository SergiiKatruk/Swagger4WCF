using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Swagger4WCF.Data
{
	class TypeData
	{
		public TypeDefinition Type { get; }
		public List<MethodData> Methods { get; }
		public string Name { get; }
		public string Description { get; }
		public List<PropertyData> Properties { get; }
		private Documentation documentation;

		public TypeData(TypeDefinition type, Documentation documentation)
		{
			this.Type = type.IsArray ? type.GetElementType().Resolve() : type;
			this.Name = this.Type.Name;
			this.Methods = new List<MethodData>();
			foreach (var methodDefinition in type.Methods.Where(_Method => _Method.IsPublic && !_Method.IsStatic && 
				(_Method.GetCustomAttribute<OperationContractAttribute>() != null || 
				_Method.CustomAttributes.Any(attr => attr.AttributeType.Name.Contains("JsonOperationContract")))))
			{
				this.Methods.Add(new MethodData(methodDefinition, documentation));
			}
			this.Properties = new List<PropertyData>();
			foreach(var prop in this.Type.Properties)
			{
				this.Properties.Add(new PropertyData(prop, documentation));
			}

			this.IsValueType = this.Type == this.Type.Module.ImportReference(typeof(void)).Resolve() ||
							this.Type == this.Type.Module.ImportReference(typeof(bool)).Resolve() ||
							this.Type == this.Type.Module.ImportReference(typeof(string)).Resolve() ||
							this.Type == this.Type.Module.ImportReference(typeof(int)).Resolve() ||
							this.Type == this.Type.Module.ImportReference(typeof(long)).Resolve() ||
							this.Type == this.Type.Module.ImportReference(typeof(DateTime)).Resolve();
			this.Description = documentation[this.Type];
			this.IsEnum = type.IsEnum;
			this.documentation = documentation;
		}

		public List<string> GetEnumValuesDescription()
		{
			List<string> enumValues = new List<string>();
			foreach (var field in this.Type.Fields)
			{
				if (field.Name == "value__")
					continue;
				var xmlDoc = this.documentation[field] ?? string.Empty;
				if (!string.IsNullOrWhiteSpace(xmlDoc))
					xmlDoc = $"({xmlDoc})";
				enumValues.Add($"{field.Constant} - {field.Name}{xmlDoc}");
			}

			return enumValues;
		}

		public bool IsValueType { get; }
		public bool IsEnum { get; }

		public override bool Equals(object obj)
		{
			return this.Type.FullName == (obj as TypeData)?.Type?.FullName || base.Equals(obj);
		}
	}
}
