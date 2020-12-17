using System.Collections.Generic;
using Swagger4WCF.Core.Information;
using Swagger4WCF.Core.Interfaces;

namespace Swagger4WCF.Core.DocumentedItems
{
	public class MethodItem : IDocumentedItem
	{
		public List<ParameterItem> Parameters { get; protected set; } = new List<ParameterItem>();
		public TypeItem ReturnType { get; protected set; }

		public string Description { get; protected set; }
		public string Name { get; protected set; }
		public string Summary { get; protected set; }
		public string Tag { get; protected set; }

		public WebInvokeDetails WebInvoke { get; protected set; }
		public List<ResponseDetails> ResponseInfos { get; protected set; }
		public string ResponceContent { get; protected set; }
	}
}
