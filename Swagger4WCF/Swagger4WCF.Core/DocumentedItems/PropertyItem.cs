using Swagger4WCF.Core.Interfaces;

namespace Swagger4WCF.Core.DocumentedItems
{
	public class PropertyItem : IDocumentedItem
	{
		public TypeItem Type { get; protected set; }
		public TypeItem DeclaringType { get; set; }
		
		public bool IsNullable { get; protected set; }
		public string Description { get; protected set; }
		public string Name { get; protected set; }
		public string DefaultValue { get; protected set; }
		public string MaxLength { get; protected set; }
		public bool IsRequired { get; protected set; }
	}
}
