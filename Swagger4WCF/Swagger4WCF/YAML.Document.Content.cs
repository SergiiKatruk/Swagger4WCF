using Mono.Cecil;
using Swagger4WCF.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel.Web;
using System.Text;

namespace Swagger4WCF
{
	static public partial class YAML
	{
		public partial class Document
		{
			private partial class Content
			{
				private StringBuilder m_Builder = new StringBuilder();
				private Tabulation m_Tabulation = new Tabulation("  ", 0);
				private Dictionary<string, TypeData> definitionList = new Dictionary<string, TypeData>();
				private HashSet<string> addedTypes = new HashSet<string>();

				static public Document Generate(TypeDefinition type, Documentation documentation, AssemblyDefinition assembly)
				{
					return new Document(type, new Content(type, documentation, assembly));
				}

				static public implicit operator string(Content compiler)
				{
					return compiler == null ? null : compiler.ToString();
				}

				public override string ToString()
				{
					return this.m_Builder.ToString();
				}

				private Content(TypeDefinition type, Documentation documentation, AssemblyDefinition assembly)
				{
					this.Add("openapi: '3.0.3'");
					this.Add("info:");
					using (new Block(this))
					{
						this.Add("title: ", type.Name);
						var _customAttribute = type.Module.Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
						var _argument = _customAttribute?.Argument<string>(0);
						if (_argument != null)
						{
							this.Add($"version: \"{ _argument }\"");
						}
					}
					this.Add("servers:");
					using (new Block(this))
					{
						this.Add($"- url: http://localhost/{type.Name}");
						this.Add($"- url: https://localhost/{type.Name}");
					}
					this.Add("paths:");

					var _allMethods = new List<MethodData>();
					{
						var allTypes = new List<TypeDefinition> { type };
						allTypes.AddRange(type.Interfaces.Select(interf => assembly.MainModule.GetType(interf.InterfaceType.FullName)));
						allTypes.ForEach(currentTypes => _allMethods.AddRange(AddMethodsForType(currentTypes, documentation)));
					}
					this.Add("components:");
					using (new Block(this))
					{
						this.Add("schemas:");
						AddDefinitionsForMethods(documentation, _allMethods);
					}
				}

				private void AddDefinitionsForMethods(Documentation documentation, List<MethodData> _methods)
				{
					using (new Block(this))
					{
						foreach (var type in _methods.Select(_Method => _Method.ReturnType).Distinct()
							.OrderBy(typeRef => typeRef.Name)
							.Where(typeRef => !typeRef.IsValueType && typeRef.Name != "Stream"))
							definitionList[type.Type.FullName] = type;

						foreach (var type in _methods.SelectMany(_Method => _Method.Parameters).Select(x => x.TypeData)
							.OrderBy(typeRef => typeRef.Name)
							.Where(typeRef => !typeRef.IsValueType && typeRef.Name != "Stream"))
							definitionList[type.Type.FullName] = type;

						while (this.definitionList.Any())
						{
							var currentDef = this.definitionList;
							this.definitionList = new Dictionary<string, TypeData>();
							foreach (var def in currentDef)
							{
								this.addedTypes.Add(def.Key);
								ParseComplexType(def.Value, documentation);
							}
						}
					}
				}

				private List<MethodData> AddMethodsForType(TypeDefinition type, Documentation documentation)
				{
					var typeData = new TypeData(type, documentation);
					var methodsGroupedByPath = typeData.Methods.GroupBy(m => m.WebInvoke.UriTemplate, key => key).ToDictionary(k => k.Key, v => v);
					using (new Block(this))
					{
						foreach (var _method in methodsGroupedByPath.Keys)
						{
							if (string.IsNullOrWhiteSpace(_method))
								continue;

							this.Add("/", _method, ":");
							foreach (var m in methodsGroupedByPath[_method])
								this.Add(m, documentation);
						}
					}

					return typeData.Methods;
				}

				private void Add(params string[] line)
				{
					this.m_Builder.AppendLine(this.m_Tabulation.ToString() + string.Concat(line));
				}

				private void Add(MethodData method, Documentation documentation)
				{
					if (method.WebInvoke == null || string.IsNullOrWhiteSpace(method.WebInvoke.Method))
						return;

					using (new Block(this))
					{
						this.Add(method.WebInvoke.Method.ToLower(), ":");

						using (new Block(this))
						{
							if (!string.IsNullOrWhiteSpace(method.Summary))
							{
								this.Add("summary: ", method.Summary);
							}
							string responseFormat = "application/json:";

							if (method.Parameters.Count == 1 && method.Parameters[0].IsStream)
								responseFormat = "multipart/form-data:";
							else if (method.WebInvoke.ResponseFormat == WebMessageFormat.Xml)
								responseFormat = "application/xml:";
							var parameters = method.Parameters.Where(param => !param.InRequestBody);
							var bodyParameters = method.Parameters.Where(param => param.InRequestBody);
							if (bodyParameters.Count() > 1)
								throw new Exception($"It isn't allowed to have multiply body parameters! Method: '{method.MethodDefinition.FullName}'.");
							if (parameters.Any())
							{
								this.Add("parameters:");
								using (new Block(this))
									foreach (ParameterData _parameter in parameters)
										this.Add(_parameter, documentation);
							}
							if (bodyParameters.Any())
							{
								this.Add("requestBody:");
								this.AddRequestBody(bodyParameters.First(), documentation, responseFormat);
							}

							this.Add("tags:");
							using (new Block(this))
							{
								this.Add("- ", method.Tag);
							}
							this.Add("responses:");
							using (new Block(this))
							{
								if (!method.ResponseInfos.Any())
								{
									this.Add("200:");
									using (new Block(this))
									{
										if (!string.IsNullOrWhiteSpace(method.Description))
										{
											this.Add("description: ", method.Description);
										}
										else
										{
											this.Add("description: OK");
										}
										if (method.MethodDefinition.ReturnType.Resolve() != method.MethodDefinition.Module.ImportReference(typeof(void)).Resolve())
										{
											this.Add("content:");
											using (new Block(this))
											{
												this.Add(responseFormat);
												using (new Block(this))
												{
													this.Add("schema:");
													using (new Block(this))
													{
														this.Add(method.MethodDefinition.ReturnType, documentation);
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
										this.Add($"{response.Code}:");
										using (new Block(this))
										{
											this.Add($"description: {response.Description}");

											if (method.MethodDefinition.ReturnType.Resolve() != method.MethodDefinition.Module.ImportReference(typeof(void)).Resolve())
											{
												this.Add("content:");
												using (new Block(this))
												{
													this.Add(responseFormat);
													using (new Block(this))
													{
														this.Add("schema:");
														using (new Block(this))
														{
															if (response.Code == 200)
															{
																this.Add(method.MethodDefinition.ReturnType, documentation);
															}
															else
															{
																this.Add("type: string");
															}
														}
													}
												}
											}
										}
									}
								}

								this.Add("default:");
								using (new Block(this))
								{
									this.Add("description: failed");
									this.Add("content:");
									using (new Block(this))
									{
										this.Add(responseFormat);
										using (new Block(this))
										{
											this.Add("schema:");
											using (new Block(this))
												this.Add("type: string");
										}
									}
								}
							}
						}
					}
				}

				private void AddRequestBody(ParameterData parameter, Documentation documentation, string responseFormat)
				{
					using (new Block(this))
					{
						if (!string.IsNullOrWhiteSpace(parameter.Description))
							this.Add("description: ", parameter.Description);
						this.Add("required: ", parameter.IsRequired.ToString());
						this.Add("content:");
						using (new Block(this))
						{
							this.Add(responseFormat);
							using (new Block(this))
							{
								this.Add("schema:");
								using (new Block(this))
									this.Add(parameter.Type, documentation);
							}
						}
					}
				}

				private void Add(ParameterData parameter, Documentation documentation)
				{
					this.Add("- name: ", parameter.Name);
					using (new Block(this))
					{
						if (parameter.IsNullable)
						{
							this.Add("in: query");
							if (!string.IsNullOrWhiteSpace(parameter.Description))
							{
								string xmlDoc = parameter.Description;
								List<string> enumValues = parameter.TypeData.GetEnumValuesDescription();
								xmlDoc += $" {string.Join(", ", enumValues.ToArray())}.";

								this.Add("description: ", xmlDoc);
							}
							this.Add("required: ", parameter.IsRequired.ToString());
							this.Add("schema:");
							using (new Block(this))
								this.Add(parameter.Type, documentation);
						}
						else if (parameter.IsValueType)
						{
							if (parameter.IsInPath)
								this.Add("in: path");
							else
								this.Add("in: query");
							if (!string.IsNullOrWhiteSpace(parameter.Description))
							{
								this.Add("description: ", parameter.Description);
							}
							this.Add("required: ", parameter.IsRequired ? "true" : "false");
							this.Add("schema:");
							using (new Block(this))
								this.Add(parameter.Type, documentation);
						}
						else
						{
							if (!string.IsNullOrWhiteSpace(parameter.Description))
							{
								this.Add("description: ", parameter.Description);
							}
							this.Add("required: ", parameter.IsRequired.ToString());
							if (parameter.IsStream)
							{
								this.Add("in: formData");
								this.Add("type: file");
							}
							else
							{
								this.Add("in: body");
								this.Add("schema:");
								using (new Block(this))
								{
									this.Add(parameter.Type, documentation);
								}
							}
						}
					}
				}

				private void Add(PropertyData property, Documentation documentation)
				{
					if (property.IsNullable)
						return;

					this.Add(property.Name, ":");
					using (new Block(this))
					{
						this.Add(property.Property.PropertyType, documentation);
						string description = property.Description;
						if (property.TypeData.IsEnum)
						{
							List<string> enumValues = property.TypeData.GetEnumValuesDescription();
							description += $" {string.Join(", ", enumValues.ToArray())}.";
						}
						this.Add("description: ", description);
						this.AddPropertyDefaultValue(property);
						this.AddPropertyMaxLength(property);
					}
				}

				private void AddPropertyMaxLength(PropertyData property)
				{
					if (!string.IsNullOrWhiteSpace(property.MaxLength))
						this.Add("maxLength: ", property.MaxLength);
				}

				private void AddPropertyDefaultValue(PropertyData property)
				{
					//if (!string.IsNullOrWhiteSpace(property.DefaultValue))
					//	this.Add("default: ", $"{property.DefaultValue}");
				}

				private void Add(TypeReference type, Documentation documentation)
				{
					if (type is GenericInstanceType genericType)
					{
						type = genericType.GenericArguments[0].GetElementType();
					}
					if (type.Resolve() == type.Module.ImportReference(typeof(string)).Resolve())
					{
						this.Add("type: \"string\"");
					}
					else if (type.Resolve() == type.Module.ImportReference(typeof(bool)).Resolve()
							 || type.Resolve() == type.Module.ImportReference(typeof(bool?)).Resolve())
					{
						this.Add("type: \"boolean\"");
					}
					else if (type.Resolve() == type.Module.ImportReference(typeof(int)).Resolve())
					{
						this.Add("type: \"number\"");
						this.Add("format: int32");
					}
					else if (type.Resolve() == type.Module.ImportReference(typeof(short)).Resolve())
					{
						this.Add("type: \"number\"");
						this.Add("format: int16");
					}
					else if (type.Resolve() == type.Module.ImportReference(typeof(long)).Resolve())
					{
						this.Add("type: \"number\"");
						this.Add("format: int32");
					}
					else if (type.Resolve() == type.Module.ImportReference(typeof(decimal)).Resolve()
							 || type.Resolve() == type.Module.ImportReference(typeof(decimal?)).Resolve())
					{
						this.Add("type: \"number\"");
						this.Add("format: decimal(9,2)");
					}
					else if (type.Resolve() == type.Module.ImportReference(typeof(DateTime)).Resolve()
							 || type.Resolve() == type.Module.ImportReference(typeof(DateTime?)).Resolve())
					{
						this.Add("type: \"string\"");
						this.Add("format: date-time");
					}
					else if (type.Resolve() == type.Module.ImportReference(typeof(Stream)).Resolve())
					{
						this.Add("type: \"string\"");
						this.Add("format: binary");
					}
					else if (type.IsArray)
					{
						this.Add("type: array");
						this.Add("items:");
						using (new Block(this)) { this.Add(type.GetElementType(), documentation); }
					}
					else if (type is TypeDefinition typeDef && typeDef.IsEnum)
					{
						this.Add("type: number");
						this.Add("enum:");

						foreach (var field in typeDef.Fields)
						{
							if (field.Name == "value__")
								continue;
							this.Add($"- {field.Constant}");
						}
					}
					else
					{
						if (type.Resolve()?.GetCustomAttribute<DataContractAttribute>() != null)
						{
							if (!this.addedTypes.Contains(type.FullName))
							{
								this.addedTypes.Add(type.FullName);
								definitionList[type.FullName] = new TypeData(type.Resolve(), documentation);
							}
							this.Add("$ref: \"#/components/schemas/", type.Name, "\"");
						}
						else
						{
							this.Add("$ref: \"#/components/schemas/", type.Name, "\"");
						}
					}
				}

				private void ParseComplexType(TypeData type, Documentation documentation)
				{
					if (type.Name == "Nullable`1")
						return;
					this.Add(type.Name, ":");
					using (new Block(this))
					{
						this.Add("type: object");
						if (!string.IsNullOrEmpty(type.Description))
						{
							this.Add(string.Concat("description: ", type.Description));
						}

						if (type.Type.Properties.Count > 0)
						{
							var requiredProperties = new List<string>();
							this.Add("properties:");
							using (new Block(this))
							{
								var propertyDefinitions = type.Properties;

								foreach (var propertyDefinition in propertyDefinitions)
								{
									if (propertyDefinition.IsRequired)
										requiredProperties.Add(propertyDefinition.Name);
									this.Add(propertyDefinition, documentation);
								}
							}
							if (requiredProperties.Any())
							{
								this.Add($"required: [{string.Join(",", requiredProperties)}]");
							}
						}
					}
				}
			}
		}
	}
}
