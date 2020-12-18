namespace Swagger4WCF.Core.Interfaces
{
	public interface IYAMLContentWriter<T> where T : IDocumentedItem
	{
		void Write(T dataObject, IYAMLContent content);
	}
}
