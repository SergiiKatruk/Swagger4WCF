﻿using Swagger4WCF.Constants;
using Swagger4WCF.Data;
using Swagger4WCF.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Swagger4WCF.YAML.Writers
{
	public class TypeWriter : IYAMLContentWriter<TypeData>
	{
		private static TypeWriter instance;

		private TypeWriter() { }

		public static TypeWriter Instance => instance ?? (instance = new TypeWriter());

		public void Write(TypeData type, IYAMLContent content)
		{
			content.Add(type.Name, ":");
			using (new Block(content))
			{
				content.Add("type: object");
				if (!string.IsNullOrEmpty(type.Description))
				{
					content.Add(string.Concat("description: ", type.Description));
				}

				if (type.Type.Properties.Count > 0)
				{
					var requiredProperties = new List<string>();
					content.Add("properties:");
					using (new Block(content))
					{
						var propertyDefinitions = type.Properties;

						foreach (var propertyDefinition in propertyDefinitions)
						{
							if (propertyDefinition.IsRequired)
								requiredProperties.Add(propertyDefinition.Name);
							PropertyWriter.Instance.Write(propertyDefinition, content);
						}
					}
					if (requiredProperties.Any())
					{
						content.Add($"required: [{string.Join(",", requiredProperties)}]");
					}
				}
			}
		}
	}
}