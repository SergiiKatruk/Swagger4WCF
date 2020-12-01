﻿using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.Serialization;
using System.ServiceModel;
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
				private List<TypeReference> definitionList = new List<TypeReference>();

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
					this.Add("swagger: '2.0'");
					this.Add("info:");
					using (new Block(this))
					{
						this.Add("title: ", type.Name);
						if (documentation[type] != null) { this.Add("description: ", documentation[type]); }
						var _customAttribute = type.Module.Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
						var _argument = _customAttribute?.Argument<string>(0);
						if (_argument != null)
						{
							this.Add($"version: \"{ _argument }\"");
						}
					}
					this.Add("host: localhost");
					this.Add("schemes:");
					using (new Block(this))
					{
						this.Add("- http");
						this.Add("- https");
					}
					this.Add("basePath: /", type.Name);
					this.Add("paths:");

					var _allMethods = new List<MethodDefinition>();
					{
						var allTypes = new List<TypeDefinition> { type };
						allTypes.AddRange(type.Interfaces.Select(interf => assembly.MainModule.GetType(interf.InterfaceType.FullName)));
						allTypes.ForEach(currentTypes => _allMethods.AddRange(AddMethodsForType(currentTypes, documentation)));
					}
					this.Add("definitions:");
					AddDefinitionsForMethods(documentation, _allMethods);
				}

				private void AddDefinitionsForMethods(Documentation documentation, List<MethodDefinition> _methods)
				{
					using (new Block(this))
					{
						var responses = _methods.Select(_Method => _Method.ReturnType).Distinct()
							.OrderBy(typeRef => typeRef.Name)
							.Where(typeRef =>
								!(typeRef.Resolve() == typeRef.Resolve().Module.ImportReference(typeof(void)).Resolve())
								&& !(typeRef.Resolve() == typeRef.Resolve().Module.ImportReference(typeof(bool)).Resolve())
								&& !(typeRef.Resolve() ==
									 typeRef.Resolve().Module.ImportReference(typeof(string)).Resolve())
								&& !(typeRef.Resolve() == typeRef.Resolve().Module.ImportReference(typeof(int)).Resolve())
								&& !(typeRef.Resolve() == typeRef.Resolve().Module.ImportReference(typeof(long)).Resolve())
								&& !(typeRef.Resolve() == typeRef.Resolve().Module.ImportReference(typeof(DateTime)).Resolve()))
							.Select(_Type => _Type.IsArray ? _Type.GetElementType() : _Type).Distinct();
						definitionList.AddRange(responses);

						var resparameters = _methods.SelectMany(_Method => _Method.Parameters).Select(x => x.ParameterType)
							.OrderBy(typeRef => typeRef.Name)
							.Where(typeRef =>
								!(typeRef.Resolve() == typeRef.Resolve().Module.ImportReference(typeof(void)).Resolve())
								&& !(typeRef.Resolve() == typeRef.Resolve().Module.ImportReference(typeof(bool)).Resolve())
								&& !(typeRef.Resolve() ==
									 typeRef.Resolve().Module.ImportReference(typeof(string)).Resolve())
								&& !(typeRef.Resolve() == typeRef.Resolve().Module.ImportReference(typeof(int)).Resolve())
								&& !(typeRef.Resolve() == typeRef.Resolve().Module.ImportReference(typeof(long)).Resolve())
								&& !(typeRef.Resolve() == typeRef.Resolve().Module.ImportReference(typeof(DateTime)).Resolve()))
							.Select(_Type => _Type.IsArray ? _Type.GetElementType() : _Type).Distinct();

						definitionList.AddRange(resparameters);
						definitionList = definitionList.Distinct().ToList();
					 	var stType = definitionList.FirstOrDefault(t => t.Name == "Stream");
						definitionList.Remove(stType);
						int beforeCnt = definitionList.Count;
						for (int i = 0; i < beforeCnt; i++)
						{
							ParseComplexType(definitionList[i], documentation);
						}

						int afterCnt = definitionList.Count;

						for (int i = beforeCnt; i < afterCnt; i++)
						{
							ParseComplexType(definitionList[i], documentation);
						}
					}
				}

				private MethodDefinition[] AddMethodsForType(TypeDefinition type, Documentation documentation)
				{
					var _methods = type.Methods.Where(_Method => _Method.IsPublic && !_Method.IsStatic && (
					_Method.GetCustomAttribute<OperationContractAttribute>() != null) || _Method.CustomAttributes.Any(attr => attr.AttributeType.Name.Contains("JsonOperationContract"))).OrderBy(_Method => _Method.MetadataToken.ToInt32()).ToArray();
					var errorAttributes = type.CustomAttributes.Where(attr => attr.AttributeType.Name == "ResponseAttribute").ToList();
					var methodsGroupedByPath = _methods.GroupBy(m =>
					{
						var attribute = m.GetCustomAttribute<WebInvokeAttribute>() ?? m.GetCustomAttribute<WebGetAttribute>();
						if (attribute == null)
							return string.Empty;

						var uriTemplate = attribute?.Value<string>("UriTemplate");
						if (uriTemplate.IndexOf('?') > 0)
							return uriTemplate.Substring(0, uriTemplate.IndexOf('?'));
						return uriTemplate;
					}, key => key).ToDictionary(k => k.Key, v => v);
					using (new Block(this))
					{
						foreach (var _method in methodsGroupedByPath.Keys)
						{
							if (string.IsNullOrWhiteSpace(_method))
								continue;

							this.Add("/", _method, ":");
							foreach (var m in methodsGroupedByPath[_method])
								this.Add(m, documentation, errorAttributes);
						}
					}

					return _methods;
				}

				private void Add(params string[] line)
				{
					this.m_Builder.AppendLine(this.m_Tabulation.ToString() + string.Concat(line));
				}

				private void Add(MethodDefinition method, Documentation documentation, List<CustomAttribute> typeResponseAttributes)
				{

					var methodResponseAttributes = method.CustomAttributes.Where(attr => attr.AttributeType.Name == "ResponseAttribute").ToList();
					var methodCodes = new HashSet<int>(methodResponseAttributes.Select(attr => attr.Value<int>("Code")));
					methodResponseAttributes.AddRange(typeResponseAttributes.Where(attr => !methodCodes.Contains(attr.Value<int>("Code"))));

					using (new Block(this))
					{
						var _attribute = method.GetCustomAttribute<WebInvokeAttribute>();
						if (_attribute == null)
						{
							_attribute = method.GetCustomAttribute<WebGetAttribute>();
							if (_attribute == null)
							{
								return;
							}
						}

						if (string.IsNullOrEmpty(_attribute.Value<string>("Method")))
						{
							return;
						}

						//this.Add("/", _attribute.Value<string>("UriTemplate") ?? method.Name, ":");

						this.Add(_attribute.Value<string>("Method").ToLower(), ":");
						var _parameters = method.Parameters;

						using (new Block(this))
						{
							if (documentation != null && documentation[method].Summary != null)
							{
								this.Add("summary: ", documentation[method].Summary);
							}
							this.Add("consumes:");
							using (new Block(this))
							{
								if (_attribute.Value("RequestFormat") && _attribute.Value<WebMessageFormat>("RequestFormat") == WebMessageFormat.Json) { this.Add("- application/json"); }
								else { this.Add("- application/xml"); }
							}
							this.Add("produces:");
							using (new Block(this))
							{
								var okResponceAttr = methodResponseAttributes.FirstOrDefault(attr => attr.Value<int>("Code") == 200);
								foreach (var attrib in methodResponseAttributes.Where(attr => attr.Value("ContentType") == true))
								{
									this.Add($"- {attrib.Value<string>("ContentType")}");
								}
								
								if (_attribute.Value("ResponseFormat") && _attribute.Value<WebMessageFormat>("ResponseFormat") == WebMessageFormat.Json) { this.Add("- application/json"); }
								else { this.Add("- application/xml"); }
							}
							if (_parameters.Count > 0)
							{
								this.Add("parameters:");
								using (new Block(this))
								{
									foreach (var _parameter in _parameters)
									{
										this.Add(method, _parameter, documentation);
									}
								}
								this.Add("tags:");
								using (new Block(this))
								{
									var attribute = method.DeclaringType.GetCustomAttribute<ServiceContractAttribute>();
									this.Add("- ", attribute?.Value<string>("Name") ?? method.DeclaringType.Name);
								}
							}
							this.Add("responses:");
							using (new Block(this))
							{
								if (!methodResponseAttributes.Any())
								{
									this.Add("200:");
									using (new Block(this))
									{
										if (documentation != null && documentation[method].Response != null)
										{
											this.Add("description: ", documentation[method].Response);
										}
										else
										{
											this.Add("description: OK");
										}
										if (method.ReturnType.Resolve() != method.Module.ImportReference(typeof(void)).Resolve())
										{
											this.Add("schema:");
											using (new Block(this))
											{
												this.Add(method.ReturnType, documentation);
											}
										}
									}
								}
								else
								{
									foreach (var attr in methodResponseAttributes)
									{
										var returnCode = attr.Value<int>("Code");
										this.Add($"{returnCode}:");
										using (new Block(this))
										{
											this.Add($"description: {attr.Value<string>("Description")}");

											if (method.ReturnType.Resolve() != method.Module.ImportReference(typeof(void)).Resolve())
											{
												this.Add("schema:");
												using (new Block(this))
												{
													if (returnCode == 200)
													{
														this.Add(method.ReturnType, documentation);
													}
													else
													{
														this.Add("type: \"string\"");
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
									this.Add("schema:");
									using (new Block(this)) { this.Add("type: \"string\""); }
								}
							}
						}
					}
				}

				private void Add(MethodDefinition method, ParameterDefinition parameter, Documentation documentation)
				{
					var _type = parameter.ParameterType;
					if (parameter.ParameterType is GenericInstanceType genericInstanceType)
						_type = genericInstanceType.GenericArguments[0];
					this.Add("- name: ", parameter.Name);
					using (new Block(this))
					{
						if (_type is TypeDefinition typeDef && typeDef.IsEnum)
						{
							this.Add("in: query");
							if (documentation != null && documentation[method, parameter] != null)
							{
								string xmlDoc = documentation[method, parameter];
								List<string> enumValues = GetEnumValuesDescription(typeDef, documentation);
								xmlDoc += $" {string.Join(", ", enumValues.ToArray())}.";

								this.Add("description: ", xmlDoc);
							}
							this.Add("required: ", parameter.ParameterType.FullName.Contains("System.Nullable") ? "false" : 
								parameter.ParameterType.IsValueType.ToString().ToLower());
							this.Add(parameter.ParameterType, documentation);
						}
						else if (_type.Resolve() == _type.Module.ImportReference(typeof(string)).Resolve()
							|| _type.Resolve() == _type.Module.ImportReference(typeof(int)).Resolve()
							|| _type.Resolve() == _type.Module.ImportReference(typeof(short)).Resolve()
							|| _type.Resolve() == _type.Module.ImportReference(typeof(long)).Resolve()
							|| _type.Resolve() == _type.Module.ImportReference(typeof(DateTime)).Resolve()
							|| _type.IsArray)
						{
							this.Add("in: query");
							if (documentation != null && documentation[method, parameter] != null) { this.Add("description: ", documentation[method, parameter]); }
							this.Add("required: ", parameter.ParameterType.FullName.Contains("System.Nullable") ? "false" :
								parameter.ParameterType.IsValueType.ToString().ToLower());
							this.Add(parameter.ParameterType, documentation);
						}
						else
						{
							this.Add("in: body");
							if (documentation != null && documentation[method, parameter] != null)
							{
								this.Add("description: ", documentation[method, parameter]);
							}
							this.Add("required: ", parameter.ParameterType.FullName.Contains("System.Nullable") ? "false" :
								parameter.ParameterType.IsValueType.ToString().ToLower());
							this.Add("schema:");
							using (new Block(this))
							{
								this.Add(parameter.ParameterType, documentation);
							}
						}
					}
				}

				private List<string> GetEnumValuesDescription(TypeDefinition typeDef, Documentation documentation)
				{
					List<string> enumValues = new List<string>();
					foreach (var field in typeDef.Fields)
					{
						if (field.Name == "value__")
							continue;
						var xmlDoc = documentation[field] ?? string.Empty;
						if (!string.IsNullOrWhiteSpace(xmlDoc))
							xmlDoc = $"({xmlDoc})";
						enumValues.Add($"{field.Constant} - {field.Name}{xmlDoc}");
					}

					return enumValues;
				}

				private void Add(PropertyDefinition property, Documentation documentation)
				{
					if (property.FullName == "T System.Nullable`1::Value()")
						return;
					this.Add(property.Name, ":");
					using (new Block(this))
					{
						this.Add(property.PropertyType, documentation);
						string description = documentation[property] ?? string.Empty;
						var type = property.PropertyType;
						if (type is GenericInstanceType genericType)
						{
							type = genericType.GenericArguments[0].GetElementType();
						}
						if (type is TypeDefinition typeDef && typeDef.IsEnum)
						{
							List<string> enumValues = GetEnumValuesDescription(typeDef, documentation);
							description += $" {string.Join(", ", enumValues.ToArray())}.";
						}
						this.Add("description: ", description);
						this.AddPropertyDefaultValue(property);
						this.AddPropertyMaxLength(property);
					}
				}

				private void AddPropertyMaxLength(PropertyDefinition property)
				{
					string maxLength = this.GetPropertyMaxLength(property);
					if (!string.IsNullOrWhiteSpace(maxLength))
						this.Add("maxLength: ", maxLength);
				}

				private void AddPropertyDefaultValue(PropertyDefinition property)
				{
					string defaultValue = this.GetPropertyDefaultValue(property);
					if (!string.IsNullOrWhiteSpace(defaultValue))
						this.Add("default: ", $"\"{defaultValue}\"");
				}

				private string GetPropertyDefaultValue(PropertyDefinition propertyDefinition) =>
					propertyDefinition.GetCustomAttribute<DefaultValueAttribute>()?.ConstructorArguments[0].Value.ToString();

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
					catch(Exception)
					{ }
					return null;
				}

				private void Add(TypeReference type, Documentation documentation)
				{
					if(type is GenericInstanceType genericType)
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
						if (type.Resolve()?.GetCustomAttribute<DataContractAttribute>() == null)
						{
							definitionList.Add(type);
							this.Add("$ref: \"#/definitions/", type.Name, "\"");
						}
						else
						{
							this.Add("$ref: \"#/definitions/", type.Name, "\"");
						}
					}
				}

				private void ParseComplexType(TypeReference referenceType, Documentation documentation)
				{
					if (referenceType.Resolve() == referenceType.Module.ImportReference(typeof(void)).Resolve())
					{
						return;
					}

					if (referenceType.Resolve() == null)
					{
						return;
					}
					if (referenceType.IsGenericInstance || referenceType.IsGenericParameter)
						return;

					this.Add(referenceType.Name, ":");
					using (new Block(this))
					{
						this.Add("type: object");
						if (documentation != null && !String.IsNullOrEmpty(documentation[referenceType.Resolve()]))
						{
							this.Add(string.Concat("description: ", documentation[referenceType.Resolve()]));
						}

						if (referenceType.Resolve().Properties.Count > 0)
						{
							var requiredProperties = new List<string>();
							this.Add("properties:");
							using (new Block(this))
							{
								var propertyDefinitions = referenceType.Resolve().Properties;

								foreach (var propertyDefinition in propertyDefinitions)
								{
									if (this.IsRequired(propertyDefinition))
										requiredProperties.Add(propertyDefinition.Name);
									this.Add(propertyDefinition, documentation);
								}
							}
							if(requiredProperties.Any())
							{
								this.Add($"required: [{string.Join(",", requiredProperties)}]");
							}
						}
					}
				}

				private bool IsRequired(PropertyDefinition propertyDefinition) =>
					propertyDefinition.GetCustomAttribute<DataMemberAttribute>()?.Value<bool>(nameof(DataMemberAttribute.IsRequired)) == true ||
					propertyDefinition.GetCustomAttribute<RequiredAttribute>() != null;
			}
		}
	}
}
