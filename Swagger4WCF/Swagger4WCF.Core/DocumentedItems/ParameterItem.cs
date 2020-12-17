using Swagger4WCF.Core.Interfaces;

namespace Swagger4WCF.Core.DocumentedItems
{
	public class ParameterItem : IDocumentedItem
	{
		public TypeItem Type { get; protected set; }
		public string Description { get; protected set; }
		public string Name { get; protected set; }
		public bool IsInPath { get; protected set; }
		public bool IsRequired { get; protected set; }
		public bool InRequestBody { get; protected set; }

	}
}
