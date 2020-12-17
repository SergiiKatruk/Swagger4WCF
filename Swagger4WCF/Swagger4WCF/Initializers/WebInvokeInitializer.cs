using Mono.Cecil;
using Swagger4WCF.Core.Information;
using System.ServiceModel.Web;

namespace Swagger4WCF.Initializers
{
	public static class WebInvokeInitializer
	{
		public static WebInvokeDetails InitializersWebInvokeDetails(MethodDefinition methodDefinition)
		{
			var webInvokeAttribute = methodDefinition.GetCustomAttribute<WebInvokeAttribute>();
			if (webInvokeAttribute == null)
				return null;

			var uriTemplateFull = webInvokeAttribute.Value<string>(nameof(WebInvokeAttribute.UriTemplate));
			var uriTemplate = uriTemplateFull;
			if (uriTemplateFull.IndexOf('?') > 0)
				uriTemplate = uriTemplateFull.Substring(0, uriTemplateFull.IndexOf('?'));

			var method = webInvokeAttribute.Value<string>(nameof(WebInvokeAttribute.Method));
			var bodyStyle = webInvokeAttribute.Value<WebMessageBodyStyle>(nameof(WebInvokeAttribute.BodyStyle));
			var responseFormat = webInvokeAttribute.Value<WebMessageFormat>(nameof(WebInvokeAttribute.ResponseFormat));
			var requestFormat = webInvokeAttribute.Value<WebMessageFormat>(nameof(WebInvokeAttribute.RequestFormat));
			return new WebInvokeDetails(uriTemplate, uriTemplateFull, method, bodyStyle, requestFormat, responseFormat);
		}
	}
}
