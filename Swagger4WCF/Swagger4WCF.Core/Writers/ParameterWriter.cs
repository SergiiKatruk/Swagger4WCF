﻿using Swagger4WCF.Core.DocumentedItems;
using Swagger4WCF.Core.Interfaces;
using Swagger4WCF.Core.YAML;

namespace Swagger4WCF.Core.Writers
{
	public class ParameterWriter : IYAMLContentWriter<ParameterItem>
	{
		private static ParameterWriter instance;

		private ParameterWriter() { }

		public static ParameterWriter Instance => instance ?? (instance = new ParameterWriter());

		public void Write(ParameterItem parameter, IYAMLContent content)
		{
			content.Add("- name: ", parameter.Name);
			using (new Block(content))
			{
				if (parameter.Type.IsNullable)
				{
					content.Add("in: query");
					if (!string.IsNullOrWhiteSpace(parameter.Description))
					{
						string xmlDoc = parameter.Description;
						xmlDoc += $" {string.Join(", ", parameter.Type.EnumValues.ToArray())}.";

						content.Add("description: ", xmlDoc);
					}
					content.Add("required: ", parameter.IsRequired.ToString());
					content.Add("schema:");
					using (new Block(content))
						content.Add(parameter.Type);
				}
				else if (parameter.Type.IsValueType)
				{
					if (parameter.IsInPath)
						content.Add("in: path");
					else
						content.Add("in: query");
					if (!string.IsNullOrWhiteSpace(parameter.Description))
					{
						content.Add("description: ", parameter.Description);
					}
					content.Add("required: ", parameter.IsRequired ? "true" : "false");
					content.Add("schema:");
					using (new Block(content))
						content.Add(parameter.Type);
				}
				else
				{
					if (!string.IsNullOrWhiteSpace(parameter.Description))
					{
						content.Add("description: ", parameter.Description);
					}
					content.Add("required: ", parameter.IsRequired.ToString());
					if (parameter.Type.IsStream)
					{
						content.Add("in: formData");
						content.Add("type: file");
					}
					else
					{
						content.Add("in: body");
						content.Add("schema:");
						using (new Block(content))
						{
							content.Add(parameter.Type);
						}
					}
				}
			}
		}

		public void WriteBodyParameter(ParameterItem parameter, string responseFormat, IYAMLContent content)
		{
			using (new Block(content))
			{
				if (!string.IsNullOrWhiteSpace(parameter.Description))
					content.Add("description: ", parameter.Description);
				content.Add("required: ", parameter.IsRequired.ToString());
				content.Add("content:");
				using (new Block(content))
				{
					content.Add(responseFormat);
					using (new Block(content))
					{
						content.Add("schema:");
						using (new Block(content))
							content.Add(parameter.Type);
					}
				}
			}
		}
	}
}