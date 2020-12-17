using Mono.Cecil;
using Swagger4WCF.Core.DocumentedItems;
using Swagger4WCF.YAML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;

namespace Swagger4WCF.Data
{
	public class TypeData : TypeItem
	{
		public TypeDefinition TypeDefinition { get; }
		
		public TypeData(TypeReference type, Documentation documentation, bool readProperties = true)
		{
			this.TypeDefinition = type.IsArray ? type.GetElementType().Resolve() : type.Resolve();
			this.IsNullable = this.TypeDefinition.FullName.StartsWith("System.Nullable");
			this.IsStream = this.TypeDefinition.Resolve() == this.TypeDefinition.Module.ImportReference(typeof(Stream)).Resolve();
			this.Name = this.TypeDefinition.Name;
			this.FullName = this.TypeDefinition.FullName;
			this.IsValueType = this.TypeDefinition == this.TypeDefinition.Module.ImportReference(typeof(void)).Resolve() ||
							this.TypeDefinition.IsValueType ||
							this.TypeDefinition == this.TypeDefinition.Module.ImportReference(typeof(bool)).Resolve() ||
							this.TypeDefinition == this.TypeDefinition.Module.ImportReference(typeof(string)).Resolve() ||
							this.TypeDefinition == this.TypeDefinition.Module.ImportReference(typeof(int)).Resolve() ||
							this.TypeDefinition == this.TypeDefinition.Module.ImportReference(typeof(short)).Resolve() ||
							this.TypeDefinition == this.TypeDefinition.Module.ImportReference(typeof(byte)).Resolve() ||
							this.TypeDefinition == this.TypeDefinition.Module.ImportReference(typeof(long)).Resolve() ||
							this.TypeDefinition == this.TypeDefinition.Module.ImportReference(typeof(DateTime)).Resolve() ||
							this.TypeDefinition.Name == nameof(Stream);

			this.Description = documentation[this.TypeDefinition];
			this.IsEnum = this.TypeDefinition.IsEnum;
			if (!this.IsValueType)
			{
				foreach (var methodDefinition in this.TypeDefinition.Methods.Where(_Method => _Method.IsPublic && !_Method.IsStatic &&
					(_Method.GetCustomAttribute<OperationContractAttribute>() != null ||
					_Method.CustomAttributes.Any(attr => attr.AttributeType.Name.Contains("JsonOperationContract")))))
				{
					this.Methods.Add(new MethodData(methodDefinition, documentation));
				}
				if (readProperties)
					foreach (var prop in this.TypeDefinition.Properties)
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
			foreach (var field in this.TypeDefinition.Fields)
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
