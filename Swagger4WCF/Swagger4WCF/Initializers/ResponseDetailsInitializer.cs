using Mono.Cecil;
using Swagger4WCF.Core.Information;

namespace Swagger4WCF.Initializers
{
	public static class ResponseDetailsInitializer
	{
		public static ResponseDetails InitializersResponse(CustomAttribute attribute) =>
			new ResponseDetails(
				attribute.Value<int>(nameof(ResponseDetails.Code)),
				attribute.Value<string>(nameof(ResponseDetails.ContentType)),
				attribute.Value<string>(nameof(ResponseDetails.Description)));

	}
}
