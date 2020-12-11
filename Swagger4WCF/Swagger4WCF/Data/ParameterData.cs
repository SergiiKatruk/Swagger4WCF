﻿using Mono.Cecil;
using Swagger4WCF.Interfaces;
using Swagger4WCF.YAML;
using System;
using System.IO;

namespace Swagger4WCF.Data
{
	public class ParameterData : IYAMLObject
	{
		public ParameterDefinition Parameter { get;  }
		public TypeReference Type { get; }
		private string methodPath;
		public TypeDefinition TypeDefinition { get; }
		public TypeData TypeData { get; }

		public string Description { get; }
		public string Name { get; }

		public ParameterData(MethodDefinition method, ParameterDefinition parameter, Documentation documentation, string path)
		{
			this.Parameter = parameter;
			this.methodPath = path;
			this.Type = parameter.ParameterType is GenericInstanceType genericInstanceType ? genericInstanceType.GenericArguments[0] : parameter.ParameterType;
			this.TypeDefinition = parameter.ParameterType.Resolve();
			this.TypeData = new TypeData(this.Type.Resolve(), documentation);
			this.Name = parameter.Name;
			var i = this.methodPath.IndexOf("?");
			var ind = this.methodPath.IndexOf("{" + this.Parameter.Name + "}");
			this.InRequestBody = ind < 0;
			this.Description = documentation[method, parameter];
			this.IsInPath = ind > 0 && (ind < i || i < 0);
			this.IsRequired = this.IsInPath || (parameter.ParameterType.IsValueType && !this.TypeData.IsNullable);
		}

		public bool IsInPath { get; }
		public bool IsRequired { get; }
		public bool InRequestBody { get; }
	}
}
