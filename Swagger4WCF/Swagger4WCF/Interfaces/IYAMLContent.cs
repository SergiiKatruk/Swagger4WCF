using Mono.Cecil;
using Swagger4WCF.YAML;
using Swagger4WCF.Core.DocumentedItems;

namespace Swagger4WCF.Interfaces
{
    public interface IYAMLContent
	{
		void Add(params string[] line);
		void Add(TypeItem type);
		Documentation Documentation { get; }
		Tabulation Tabulation { get; set; }
	}
}
