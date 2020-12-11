using Mono.Cecil;
using Swagger4WCF.YAML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

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

		public TypeData(TypeDefinition type, Documentation documentation, bool readProperties = true)
		{
			this.Type = type.IsArray ? type.GetElementType().Resolve() : type;
			this.Name = this.Type.Name;
			this.Methods = new List<MethodData>();
			this.Properties = new List<PropertyData>();
			this.IsValueType = this.Type == this.Type.Module.ImportReference(typeof(void)).Resolve() ||
							this.Type == this.Type.Module.ImportReference(typeof(bool)).Resolve() ||
							this.Type == this.Type.Module.ImportReference(typeof(string)).Resolve() ||
							this.Type == this.Type.Module.ImportReference(typeof(int)).Resolve() ||
							this.Type == this.Type.Module.ImportReference(typeof(long)).Resolve() ||
							this.Type == this.Type.Module.ImportReference(typeof(DateTime)).Resolve();

			this.Description = documentation[this.Type];
			this.IsEnum = type.IsEnum;
			this.documentation = documentation;
			if (!this.IsValueType)
			{
				foreach (var methodDefinition in type.Methods.Where(_Method => _Method.IsPublic && !_Method.IsStatic &&
					(_Method.GetCustomAttribute<OperationContractAttribute>() != null ||
					_Method.CustomAttributes.Any(attr => attr.AttributeType.Name.Contains("JsonOperationContract")))))
				{
					this.Methods.Add(new MethodData(methodDefinition, documentation));
				}
				if (readProperties)
					foreach (var prop in this.Type.Properties)
						this.Properties.Add(new PropertyData(prop, documentation));
			}
			this.EnumPerCaption = new Dictionary<string, object>();
			this.EnumPerValue = new Dictionary<object, object>();
			this.EnumValues = new List<string>();
			if (this.IsEnum)
				this.InitializeEnumValues();
		}
		public List<string> EnumValues { get; }
		public Dictionary<object, object> EnumPerValue {get;}
		public Dictionary<string, object> EnumPerCaption { get; }

		private void InitializeEnumValues()
		{
			foreach (var field in this.Type.Fields)
			{
				if (field.Name == "value__")
					continue;
				var xmlDoc = this.documentation[field] ?? string.Empty;
				if (!string.IsNullOrWhiteSpace(xmlDoc))
					xmlDoc = $"({xmlDoc})";
				this.EnumValues.Add($"{field.Constant} - {field.Name}{xmlDoc}");
				this.EnumPerValue[field.Constant] = field.Constant;
				this.EnumPerCaption[field.Name] = field.Constant;
			}
		}

		public bool IsValueType { get; }
		public bool IsEnum { get; }
	}
}
