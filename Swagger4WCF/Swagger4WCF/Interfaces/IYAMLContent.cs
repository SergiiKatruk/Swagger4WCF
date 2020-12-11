using Mono.Cecil;
using Swagger4WCF.YAML;

namespace Swagger4WCF.Interfaces
{
    public interface IYAMLContent
	{
		void Add(params string[] line);
		void Add(TypeReference type);
		Documentation Documentation { get; }
		Tabulation Tabulation { get; set; }
	}
}
