namespace Swagger4WCF.Core.Information
{
	public class ResponseDetails
	{
		public int Code { get; protected set; }
		public string ContentType { get; protected set; }
		public string Description { get; protected set; }
		public ResponseDetails(int code, string contentType, string description) =>
			(this.Code, this.ContentType, this.Description) = (code, contentType, description);
	}
}
