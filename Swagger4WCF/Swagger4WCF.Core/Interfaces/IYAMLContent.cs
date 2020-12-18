using Swagger4WCF.Core.YAML;
using Swagger4WCF.Core.DocumentedItems;

namespace Swagger4WCF.Core.Interfaces
{
    public interface IYAMLContent
	{
		void Add(params string[] line);
		void Add(TypeItem type);
		Tabulation Tabulation { get; set; }
	}
}
