using Mono.Cecil;
using Swagger4WCF.Data;
using Swagger4WCF.YAML;

namespace Swagger4WCF.Interfaces
{
	public interface IYAMLContent
	{
		void Add(params string[] line);
		void Add(TypeReference type);
		void AddRequestBody(ParameterData parameter, string responseFormat);
		Documentation Documentation { get; }
		Tabulation Tabulation { get; set; }
	}
}
