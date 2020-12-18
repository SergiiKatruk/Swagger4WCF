using System.Collections.Generic;
using Swagger4WCF.Core.Information;
using Swagger4WCF.Core.Interfaces;
using Swagger4WCF.Core.Constants;

namespace Swagger4WCF.Core.DocumentedItems
{
	public class TypeItem : IDocumentedItem
	{
		private string name;

		public List<MethodItem> Methods { get; protected set; } = new List<MethodItem>();
		public List<PropertyItem> Properties { get; protected set; } = new List<PropertyItem>();
		public TypeItem ElementType { get; protected set; }

		public string Name 
		{ 
			get => this.name; 
			protected set
			{
				if (this.name == value)
					return;

				this.name = value;
				this.YAMLTypeName = YAMLTypes.Convert(value);
				this.YamlTypeFormat = YAMLFormats.Convert(value);

			} 
		}
		public string FullName { get; protected set; }
		public string Description { get; protected set; }

		public bool IsValueType { get; protected set; }
		public bool IsNullable { get; protected set; }
		public bool IsStream { get; protected set; }
		public bool IsArray { get; protected set; }
		public bool IsEnum { get; protected set; }

		public List<string> EnumValues { get; protected set; }
		public Dictionary<object, object> EnumPerValue { get; protected set; }
		public Dictionary<string, object> EnumPerCaption { get; protected set; }

		public override string ToString() => this.Name;

		public string YAMLTypeName { get; private set; }
		public string YamlTypeFormat { get; private set; }

	}
}
