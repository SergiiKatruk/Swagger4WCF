using Swagger4WCF.Data;
using Swagger4WCF.Interfaces;

namespace Swagger4WCF.YAML.Writers
{
	public class PropertyWriter : IYAMLContentWriter<PropertyData>
	{
		private static PropertyWriter instance;

		private PropertyWriter() { }

		public static PropertyWriter Instance => instance ?? (instance = new PropertyWriter());

		public void Write(PropertyData property, IYAMLContent content)
		{
			content.Add(property.Name, ":");
			using (new Block(content))
			{
				content.Add(property.Property.PropertyType);
				string description = property.Description;
				if (property.TypeData.IsEnum)
					description += $" {string.Join(", ", property.TypeData.EnumValues.ToArray())}.";
				content.Add("description: ", description);
				this.AddPropertyDefaultValue(property, content);
				this.AddPropertyMaxLength(property, content);
			}
		}

		private void AddPropertyMaxLength(PropertyData property, IYAMLContent content)
		{
			if (!string.IsNullOrWhiteSpace(property.MaxLength))
				content.Add("maxLength: ", property.MaxLength);
		}

		private void AddPropertyDefaultValue(PropertyData property, IYAMLContent content)
		{
			if (!string.IsNullOrWhiteSpace(property.DefaultValue))
				content.Add("default: ", $"{property.DefaultValue}");
		}
	}
}
