using Swagger4WCF.YAML;
using Swagger4WCF.Core.Interfaces;

namespace Swagger4WCF.Interfaces
{
	public interface IYAMLContentWriter<T> where T : IDocumentedItem
	{
		void Write(T dataObject, IYAMLContent content);
	}
}
