using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Swagger4WCF.Data
{
	class PropertyData
	{
		public PropertyDefinition Property { get; }

		public PropertyData(PropertyDefinition property, Documentation documentation)
		{
			this.Property = property;
			this.IsNullable = property.PropertyType.FullName.StartsWith("System.Nullable");
			this.Name = property.Name;
			this.Description = documentation[this.Property];
			if(!this.IsNullable)
				this.TypeData = new TypeData(property.PropertyType.Resolve(), documentation);
			this.DefaultValue = this.GetPropertyDefaultValue(this.Property);
			this.MaxLength = this.GetPropertyMaxLength(this.Property);
			this.IsRequired = this.GetIsRequired(this.Property);
		}

		private string GetPropertyDefaultValue(PropertyDefinition propertyDefinition)
		{
			var val = propertyDefinition.GetCustomAttribute<DefaultValueAttribute>()?.ConstructorArguments[0].Value;
			if (val is null)
				return null;
			if (this.TypeData.IsEnum)
			{
				var enumValues = this.TypeData.GetEnumValuesDescription();
				return val.ToString();
			}
			if (val is string)
				return val as string;
			return val.ToString();
		}

		private string GetPropertyMaxLength(PropertyDefinition propertyDefinition) =>
			propertyDefinition.GetCustomAttribute<MaxLengthAttribute>()?.ConstructorArguments[0].Value.ToString() ??
			this.GetCtorValue(propertyDefinition.CustomAttributes.FirstOrDefault(attr => attr.AttributeType.Name == "JsonConverterAttribute" && attr.ConstructorArguments.Count == 2 &&
				(attr.ConstructorArguments[0].Value as TypeDefinition)?.Name.Contains("MaxLength") == true)?.ConstructorArguments[1]);

		private string GetCtorValue(CustomAttributeArgument? customAttribute)
		{
			try
			{
				if (customAttribute.HasValue)
					return ((CustomAttributeArgument)((CustomAttributeArgument)((customAttribute.Value).Value as CustomAttributeArgument[])[0]).Value).Value.ToString();
			}
			catch (Exception)
			{ }
			return null;
		}

		private bool GetIsRequired(PropertyDefinition propertyDefinition) =>
			propertyDefinition.GetCustomAttribute<DataMemberAttribute>()?.Value<bool>(nameof(DataMemberAttribute.IsRequired)) == true ||
			propertyDefinition.GetCustomAttribute<RequiredAttribute>() != null;

		public bool IsNullable { get; }
		public string Description { get; }
		public string Name { get; }
		public TypeData TypeData { get;}
		public string DefaultValue { get; }
		public string MaxLength { get; }
		public bool IsRequired { get; }
	}
}
