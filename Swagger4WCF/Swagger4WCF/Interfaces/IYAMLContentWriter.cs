using Swagger4WCF.YAML;

namespace Swagger4WCF.Interfaces
{
	public interface IYAMLContentWriter<T> where T : IYAMLObject
	{
		void Write(T dataObject, IYAMLContent content);
	}
}
