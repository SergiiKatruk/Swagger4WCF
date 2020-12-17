using Mono.Cecil;
using Swagger4WCF.Core.DocumentedItems;
using Swagger4WCF.YAML;

namespace Swagger4WCF.Data
{
	public class ParameterData : ParameterItem
	{
		public ParameterDefinition Parameter { get;  }
		public TypeReference TypeReference { get; }
		public TypeDefinition TypeDefinition { get; }
		
		public ParameterData(MethodDefinition method, ParameterDefinition parameter, Documentation documentation, string path)
		{
			this.Parameter = parameter;
			this.TypeReference = parameter.ParameterType is GenericInstanceType genericInstanceType ? genericInstanceType.GenericArguments[0] : parameter.ParameterType;
			this.TypeDefinition = parameter.ParameterType.Resolve();
			this.Type = new TypeData(this.TypeReference.Resolve(), documentation);
			this.Name = parameter.Name;
			var i = path.IndexOf("?");
			var ind = path.IndexOf("{" + this.Parameter.Name + "}");
			this.InRequestBody = ind < 0;
			this.Description = documentation[method, parameter];
			this.IsInPath = ind > 0 && (ind < i || i < 0);
			this.IsRequired = this.IsInPath || (parameter.ParameterType.IsValueType && !this.Type.IsNullable);
		}
	}
}
