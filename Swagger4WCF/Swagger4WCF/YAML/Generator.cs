using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace Swagger4WCF.YAML
{
	static public class Generator
    {
        static public IEnumerable<Document> Generate(AssemblyDefinition assembly, Documentation documentation, string interfaceName = null)
        {
            foreach (var _type in assembly.MainModule.Types.Where(_Type => _Type.IsInterface && _Type.GetCustomAttribute<ServiceContractAttribute>() != null
                && (interfaceName == null || _Type.Name.Contains(interfaceName))))
            {
                yield return Document.Generate(_type, documentation, assembly);
            }
        }
    }
}
