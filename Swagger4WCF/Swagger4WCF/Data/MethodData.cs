using Mono.Cecil;
using Swagger4WCF.Interfaces;
using Swagger4WCF.YAML;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using Swagger4WCF.Core.DocumentedItems;
using Swagger4WCF.Core.Information;
using Swagger4WCF.Initializers;

namespace Swagger4WCF.Data
{
	public class MethodData : MethodItem
	{
		public MethodDefinition MethodDefinition { get; }
		
		public MethodData(MethodDefinition methodDefinition, Documentation documentation)
		{
			this.MethodDefinition = methodDefinition;
			this.WebInvoke = WebInvokeInitializer.InitializersWebInvokeDetails(methodDefinition);
			this.InitializeDescriptionInfo(methodDefinition);
			this.InitializeResponceInfo(methodDefinition);
			foreach(var param in methodDefinition.Parameters)
				this.Parameters.Add(new ParameterData(methodDefinition, param, documentation, this.WebInvoke.UriTemplateFull));
			this.Summary = documentation[methodDefinition].Summary;
			this.Tag = methodDefinition.DeclaringType.GetCustomAttribute<ServiceContractAttribute>()?.Value<string>("Name") ?? methodDefinition.DeclaringType.Name;
			this.ReturnType = new TypeData(methodDefinition.ReturnType, documentation);
			this.InitializeResponseContent();
		}

		private void InitializeResponseContent()
		{
			string responseFormat = "application/json:";

			if (this.Parameters.Count == 1 && this.Parameters[0].Type.IsStream)
				responseFormat = "multipart/form-data:";
			else if (this.WebInvoke.ResponseFormat == WebMessageFormat.Xml)
				responseFormat = "application/xml:";

			this.ResponceContent = responseFormat;
		}

		private void InitializeResponceInfo(MethodDefinition methodDefinition)
		{
			var methodResponseAttributes = methodDefinition.CustomAttributes.Where(attr => attr.AttributeType.Name == "ResponseAttribute");
			var classResponseAttributes = methodDefinition.DeclaringType.CustomAttributes.Where(attr => attr.AttributeType.Name == "ResponseAttribute");

			var methodResponces = new List<ResponseDetails>();
			var classResponces = new List<ResponseDetails>();
			foreach (CustomAttribute attribute in methodResponseAttributes)
				methodResponces.Add(ResponseDetailsInitializer.InitializersResponse(attribute));
			foreach (CustomAttribute attribute in classResponseAttributes)
				classResponces.Add(ResponseDetailsInitializer.InitializersResponse(attribute));
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
