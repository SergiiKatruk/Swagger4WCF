using Mono.Cecil;
using Swagger4WCF.Interfaces;
using Swagger4WCF.YAML;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using Swagger4WCF.Core.DocumentedItems;

namespace Swagger4WCF.Data
{
	public class PropertyData : PropertyItem
	{
		public PropertyDefinition PropertyDefinition { get; }
		public TypeDefinition TypeDefinition { get; }

		public PropertyData(PropertyDefinition property, Documentation documentation)
		{
			this.PropertyDefinition = property;
			this.IsNullable = property.PropertyType.FullName.StartsWith("System.Nullable");
			this.Name = property.Name;
			this.Description = documentation[this.PropertyDefinition];
			if (this.IsNullable)
				this.Type = new TypeData((property.PropertyType as GenericInstanceType).GenericArguments[0].Resolve(), documentation, false);
			else
				this.Type = new TypeData(property.PropertyType.Resolve(), documentation);
			this.DefaultValue = this.GetPropertyDefaultValue(this.PropertyDefinition);
			this.MaxLength = this.GetPropertyMaxLength(this.PropertyDefinition);
			this.IsRequired = this.GetIsRequired(this.PropertyDefinition);
		}

		private string GetPropertyDefaultValue(PropertyDefinition propertyDefinition)
		{
			var val = propertyDefinition.GetCustomAttribute<DefaultValueAttribute>()?.ConstructorArguments[0].Value;
			val = (val is CustomAttributeArgument attr) ? attr.Value : val;
			if (val is null)
				return null;
			if (this.Type.IsEnum)
			{
				return this.Type.EnumPerValue.ContainsKey(val) ? this.Type.EnumPerValue[val].ToString() :
					this.Type.EnumPerCaption.ContainsKey(val.ToString()) ? this.Type.EnumPerCaption[val.ToString()].ToString(): 
					val.ToString();
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
	}
}
