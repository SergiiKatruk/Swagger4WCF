using System.ServiceModel.Web;

namespace Swagger4WCF.Core.Information
{
	public class WebInvokeDetails
	{
		public string UriTemplate { get; protected set; }
		public string UriTemplateFull { get; protected set; }
		public string Method { get; protected set; }
		public WebMessageBodyStyle BodyStyle { get; protected set; }
		public WebMessageFormat ResponseFormat { get; protected set; }
		public WebMessageFormat RequestFormat { get; protected set; }

		public WebInvokeDetails(string uriTemplate, string uriTemplateFull, string method, 
			WebMessageBodyStyle bodyStyle, WebMessageFormat requestFormat, WebMessageFormat responseFormat)
		{
			this.UriTemplate = uriTemplate;
			this.UriTemplateFull = uriTemplateFull;
			this.Method = method;
			this.BodyStyle = bodyStyle;
			this.ResponseFormat = responseFormat;
			this.RequestFormat = requestFormat;
		}
	}
}
