using Mono.Cecil;
using Swagger4WCF.Interfaces;
using Swagger4WCF.YAML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using Swagger4WCF.Core.DocumentedItems;

namespace Swagger4WCF.Data
{
	public class TypeData : TypeItem
	{
		public TypeDefinition Type { get; }
		
		public TypeData(TypeDefinition type, Documentation documentation, bool readProperties = true)
		{
			this.Type = type.IsArray ? type.GetElementType().Resolve() : type;
			this.IsNullable = this.Type.FullName.StartsWith("System.Nullable");
			this.IsStream = this.Type.Resolve() == this.Type.Module.ImportReference(typeof(Stream)).Resolve();
			this.Name = this.Type.Name;
			this.IsValueType = this.Type == this.Type.Module.ImportReference(typeof(void)).Resolve() ||
							this.Type.IsValueType ||
							this.Type == this.Type.Module.ImportReference(typeof(bool)).Resolve() ||
							this.Type == this.Type.Module.ImportReference(typeof(string)).Resolve() ||
							this.Type == this.Type.Module.ImportReference(typeof(int)).Resolve() ||
							this.Type == this.Type.Module.ImportReference(typeof(short)).Resolve() ||
							this.Type == this.Type.Module.ImportReference(typeof(byte)).Resolve() ||
							this.Type == this.Type.Module.ImportReference(typeof(long)).Resolve() ||
							this.Type == this.Type.Module.ImportReference(typeof(DateTime)).Resolve() ||
							this.Type.Name == nameof(Stream);

			this.Description = documentation[this.Type];
			this.IsEnum = type.IsEnum;
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
				this.InitializeEnumValues(documentation);
		}

		private void InitializeEnumValues(Documentation documentation)
		{
			foreach (var field in this.Type.Fields)
			{
				if (field.Name == "value__")
					continue;
				var xmlDoc = documentation[field] ?? string.Empty;
				if (!string.IsNullOrWhiteSpace(xmlDoc))
					xmlDoc = $"({xmlDoc})";
				this.EnumValues.Add($"{field.Constant} - {field.Name}{xmlDoc}");
				this.EnumPerValue[field.Constant] = field.Constant;
				this.EnumPerCaption[field.Name] = field.Constant;
			}
		}
	}
}
