﻿using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swagger4WCF.Data
{
	class ParameterData
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
			this.TypeData = new TypeData(this.TypeDefinition, documentation);
			this.Name = parameter.Name;
			var i = this.methodPath.IndexOf("?");
			var ind = this.methodPath.IndexOf("{" + this.Parameter.Name + "}");
			this.InRequestBody = ind < 0;
			this.Description = documentation[method, parameter];
			this.IsInPath = ind > 0 && (ind < i || i < 0);
			this.IsEnum = this.Type is TypeDefinition typeDef && typeDef.IsEnum;
			this.IsValueType = this.TypeDefinition == this.Type.Module.ImportReference(typeof(string)).Resolve()
							|| this.TypeDefinition == this.Type.Module.ImportReference(typeof(int)).Resolve()
							|| this.TypeDefinition == this.Type.Module.ImportReference(typeof(short)).Resolve()
							|| this.TypeDefinition == this.Type.Module.ImportReference(typeof(decimal)).Resolve()
							|| this.TypeDefinition == this.Type.Module.ImportReference(typeof(long)).Resolve()
							|| this.TypeDefinition == this.Type.Module.ImportReference(typeof(long)).Resolve()
							|| this.TypeDefinition == this.Type.Module.ImportReference(typeof(DateTime)).Resolve()
							|| this.Type.IsArray;
			this.IsNullable = parameter.ParameterType.FullName.Contains(nameof(System.Nullable));
			this.IsRequired = this.IsInPath || (parameter.ParameterType.IsValueType && !this.IsNullable);
			this.IsStream = this.Type.Resolve() == this.Type.Module.ImportReference(typeof(Stream)).Resolve();
		}

		public bool IsEnum { get; }
		public bool IsValueType { get; }
		public bool IsInPath { get; }
		public bool IsRequired { get; }
		public bool IsNullable { get; }
		public bool IsStream { get; }
		public bool InRequestBody { get; }
	}
}
