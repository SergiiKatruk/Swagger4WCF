using Swagger4WCF.Core.DocumentedItems;
using Swagger4WCF.Core.Interfaces;
using Swagger4WCF.Core.YAML;
using System;
using System.Linq;

namespace Swagger4WCF.Core.Writers
{
	public class MethodWriter : IYAMLContentWriter<MethodItem>
	{
		private static MethodWriter instance;

		private MethodWriter() { }

		public static MethodWriter Instance => instance ?? (instance = new MethodWriter());

		public void Write(MethodItem method, IYAMLContent content)
		{
			if (method.WebInvoke == null || string.IsNullOrWhiteSpace(method.WebInvoke.Method))
				return;

			using (new Block(content))
			{
				content.Add(method.WebInvoke.Method.ToLower(), ":");

				using (new Block(content))
				{
					if (!string.IsNullOrWhiteSpace(method.Summary))
					{
						content.Add("summary: ", method.Summary);
					}
					string responseFormat = method.ResponceContent;

					var parameters = method.Parameters.Where(param => !param.InRequestBody);
					var bodyParameters = method.Parameters.Where(param => param.InRequestBody);
					if (bodyParameters.Count() > 1)
						throw new Exception($"It isn't allowed to have multiply body parameters! Method: '{method.Name}'.");
					if (parameters.Any())
					{
						content.Add("parameters:");
						using (new Block(content))
							foreach (ParameterItem _parameter in parameters)
								ParameterWriter.Instance.Write(_parameter, content);
					}
					if (bodyParameters.Any())
					{
						content.Add("requestBody:");
						ParameterWriter.Instance.WriteBodyParameter(bodyParameters.First(), responseFormat, content);
					}

					content.Add("tags:");
					using (new Block(content))
					{
						content.Add("- ", method.Tag);
					}
					content.Add("responses:");
					using (new Block(content))
					{
						if (!method.ResponseInfos.Any())
						{
							content.Add("200:");
							using (new Block(content))
							{
								if (!string.IsNullOrWhiteSpace(method.Description))
								{
									content.Add("description: ", method.Description);
								}
								else
								{
									content.Add("description: OK");
								}
								if (!method.IsVoid)
								{
									content.Add("content:");
									using (new Block(content))
									{
										content.Add(responseFormat);
										using (new Block(content))
										{
											content.Add("schema:");
											using (new Block(content))
											{
												content.Add(method.ReturnType);
											}
										}
									}
								}
							}
						}
						else
						{
							foreach (var response in method.ResponseInfos)
							{
								content.Add($"{response.Code}:");
								using (new Block(content))
								{
									content.Add($"description: {response.Description}");

									if (!method.IsVoid)
									{
										content.Add("content:");
										using (new Block(content))
										{
											content.Add(responseFormat);
											using (new Block(content))
											{
												content.Add("schema:");
												using (new Block(content))
												{
													if (response.Code == 200)
													{
														content.Add(method.ReturnType);
													}
													else
													{
														content.Add("type: string");
													}
												}
											}
										}
									}
								}
							}
						}

						content.Add("default:");
						using (new Block(content))
						{
							content.Add("description: failed");
							content.Add("content:");
							using (new Block(content))
							{
								content.Add(responseFormat);
								using (new Block(content))
								{
									content.Add("schema:");
									using (new Block(content))
										content.Add("type: string");
								}
							}
						}
					}
				}
			}
		}
	}
}
