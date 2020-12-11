using Mono.Cecil;

namespace Swagger4WCF.Data
{
	class ResponseInfo
	{
        public int Code { get; }
        public string ContentType { get; }
        public string Description { get; }

        public ResponseInfo(CustomAttribute attribute)
		{
            this.Code = attribute.Value<int>(nameof(ResponseInfo.Code));
            this.ContentType = attribute.Value<string>(nameof(ResponseInfo.ContentType));
            this.Description = attribute.Value<string>(nameof(ResponseInfo.Description));
        }
    }
}
