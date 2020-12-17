using System.Collections.Generic;
using Swagger4WCF.Core.Information;
using Swagger4WCF.Core.Interfaces;

namespace Swagger4WCF.Core.DocumentedItems
{
	public class TypeItem : IDocumentedItem
	{
		public List<MethodItem> Methods { get; protected set; } = new List<MethodItem>();
		public List<PropertyItem> Properties { get; protected set; } = new List<PropertyItem>();
		
		public string Name { get; protected set; }
		public string FullName { get; protected set; }
		public string Description { get; protected set; }

		public bool IsValueType { get; protected set; }
		public bool IsNullable { get; protected set; }
		public bool IsStream { get; protected set; }
		public bool IsEnum { get; protected set; }

		public List<string> EnumValues { get; protected set; }
		public Dictionary<object, object> EnumPerValue { get; protected set; }
		public Dictionary<string, object> EnumPerCaption { get; protected set; }
		
		public override string ToString() => this.Name;

	}
}
