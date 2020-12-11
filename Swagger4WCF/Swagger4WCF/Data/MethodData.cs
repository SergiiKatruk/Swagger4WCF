using Mono.Cecil;
using Swagger4WCF.Interfaces;
using Swagger4WCF.YAML;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace Swagger4WCF.Data
{
	public class MethodData : IYAMLObject
	{
		public string Description { get; private set; }
		public string Name { get; private set; }
		public WebInvokeInfo WebInvoke { get; private set; }
		public List<ResponseInfo> ResponseInfos { get; private set; }
		public string ResponceContent { get; private set; }

		public MethodDefinition MethodDefinition { get; }
		public TypeData ReturnType { get; }

		public string Summary { get;  }
		public List<ParameterData> Parameters { get; }
		public string Tag { get; }

		public MethodData(MethodDefinition methodDefinition, Documentation documentation)
		{
			this.MethodDefinition = methodDefinition;
			this.WebInvoke = new WebInvokeInfo(methodDefinition);
			this.InitializeDescriptionInfo(methodDefinition);
			this.InitializeResponceInfo(methodDefinition);
			this.Parameters = new List<ParameterData>();
			foreach(var param in methodDefinition.Parameters)
				this.Parameters.Add(new ParameterData(methodDefinition, param, documentation, this.WebInvoke.UriTemplateFull));
			this.Summary = documentation[methodDefinition].Summary;
			this.Tag = methodDefinition.DeclaringType.GetCustomAttribute<ServiceContractAttribute>()?.Value<string>("Name") ?? methodDefinition.DeclaringType.Name;
			this.ReturnType = new TypeData(methodDefinition.ReturnType.Resolve(), documentation);
			this.InitializeResponseContent();
		}

		private void InitializeResponseContent()
		{
			string responseFormat = "application/json:";

			if (this.Parameters.Count == 1 && this.Parameters[0].IsStream)
				responseFormat = "multipart/form-data:";
			else if (this.WebInvoke.ResponseFormat == WebMessageFormat.Xml)
				responseFormat = "application/xml:";

			this.ResponceContent = responseFormat;
		}

		private void InitializeResponceInfo(MethodDefinition methodDefinition)
		{
			var methodResponseAttributes = methodDefinition.CustomAttributes.Where(attr => attr.AttributeType.Name == "ResponseAttribute");
			var classResponseAttributes = methodDefinition.DeclaringType.CustomAttributes.Where(attr => attr.AttributeType.Name == "ResponseAttribute");
			this.ResponseInfos = new List<ResponseInfo>();

			var methodResponces = new List<ResponseInfo>();
			var classResponces = new List<ResponseInfo>();
			foreach (CustomAttribute attribute in methodResponseAttributes)
				methodResponces.Add(new ResponseInfo(attribute));
			foreach (CustomAttribute attribute in classResponseAttributes)
				classResponces.Add(new ResponseInfo(attribute));
			var addedCodes = new HashSet<int>();
			methodResponces.ForEach(response =>
			{
				this.ResponseInfos.Add(response);
				addedCodes.Add(response.Code);
			});
			classResponces.ForEach(response =>
			{
				if(!addedCodes.Contains(response.Code))
					this.ResponseInfos.Add(response);
			});
		}

		private void InitializeDescriptionInfo(MethodDefinition methodDefinition)
		{
			var descriptionAttribute = methodDefinition.GetCustomAttribute<DescriptionAttribute>();

			this.Description = descriptionAttribute?.Value<string>(nameof(DescriptionAttribute.Description));
		}

	}
}
