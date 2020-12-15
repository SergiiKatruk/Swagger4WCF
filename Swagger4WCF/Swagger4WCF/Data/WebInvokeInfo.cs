using Mono.Cecil;
using System.ServiceModel.Web;
using Swagger4WCF.Core.Information;

namespace Swagger4WCF.Data
{
	public class WebInvokeInfo : WebInvokeInformation
	{
		public WebInvokeInfo(MethodDefinition methodDefinition)
		{
			var webInvokeAttribute = methodDefinition.GetCustomAttribute<WebInvokeAttribute>();
			this.InitializeWebInvokeInfo(webInvokeAttribute);
			
		}

		private void InitializeWebInvokeInfo(CustomAttribute webInvokeAttribute)
		{
			if (webInvokeAttribute == null)
				return;

			this.UriTemplateFull = webInvokeAttribute.Value<string>(nameof(WebInvokeInfo.UriTemplate));
			this.UriTemplate = this.UriTemplateFull;
			if (this.UriTemplateFull.IndexOf('?') > 0)
				this.UriTemplate = this.UriTemplateFull.Substring(0, this.UriTemplateFull.IndexOf('?'));

			this.Method = webInvokeAttribute.Value<string>(nameof(WebInvokeInfo.Method));
			this.BodyStyle = webInvokeAttribute.Value<WebMessageBodyStyle>(nameof(WebInvokeInfo.BodyStyle));
			this.ResponseFormat = webInvokeAttribute.Value<WebMessageFormat>(nameof(WebInvokeInfo.ResponseFormat));
			this.RequestFormat = webInvokeAttribute.Value<WebMessageFormat>(nameof(WebInvokeInfo.RequestFormat));
		}
	}
}
