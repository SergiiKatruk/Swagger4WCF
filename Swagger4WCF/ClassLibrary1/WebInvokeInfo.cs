using System;
using System.Collections.Generic;
using System.Text;

namespace Swager4WCF.Core.InformationItems
{
	public class WebInvokeInfo
	{
		public string UriTemplate { get; private set; }
		public string UriTemplateFull { get; private set; }
		public string Method { get; private set; }
		public WebMessageBodyStyle BodyStyle { get; private set; }
		public WebMessageFormat ResponseFormat { get; private set; }
		public WebMessageFormat RequestFormat { get; private set; }
	}
}
