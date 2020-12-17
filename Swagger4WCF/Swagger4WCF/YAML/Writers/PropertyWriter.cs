using Swagger4WCF.Core.DocumentedItems;
using Swagger4WCF.Data;
using Swagger4WCF.Interfaces;

namespace Swagger4WCF.YAML.Writers
{
	public class PropertyWriter : IYAMLContentWriter<PropertyItem>
	{
		private static PropertyWriter instance;

		private PropertyWriter() { }

		public static PropertyWriter Instance => instance ?? (instance = new PropertyWriter());

		public void Write(PropertyItem property, IYAMLContent content)
		{
			content.Add(property.Name, ":");
			using (new Block(content))
			{
				content.Add(((PropertyData)property).PropertyDefinition.PropertyType);
				string description = property.Description;
				if (property.Type.IsEnum)
					description += $" {string.Join(", ", property.Type.EnumValues.ToArray())}.";
				content.Add("description: ", description);
				this.AddPropertyDefaultValue(property, content);
				this.AddPropertyMaxLength(property, content);
			}
		}

		private void AddPropertyMaxLength(PropertyItem property, IYAMLContent content)
		{
			if (!string.IsNullOrWhiteSpace(property.MaxLength))
				content.Add("maxLength: ", property.MaxLength);
		}

		private void AddPropertyDefaultValue(PropertyItem property, IYAMLContent content)
		{
			if (!string.IsNullOrWhiteSpace(property.DefaultValue))
				content.Add("default: ", $"{property.DefaultValue}");
		}
	}
}
