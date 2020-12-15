using System.ServiceModel.Web;

namespace Swagger4WCF.Core.Information
{
	public abstract class WebInvokeInformation
	{
		public string UriTemplate { get; protected set; }
		public string UriTemplateFull { get; protected set; }
		public string Method { get; protected set; }
		public WebMessageBodyStyle BodyStyle { get; protected set; }
		public WebMessageFormat ResponseFormat { get; protected set; }
		public WebMessageFormat RequestFormat { get; protected set; }
	}
}
