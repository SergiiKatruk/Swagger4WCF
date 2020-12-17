namespace Swagger4WCF.Core.Constants
{
	class YAMLTypes
	{
		public const string Integer = "integer";
		public const string Number = "number";
		public const string String = "string";
		public const string Boolean = "boolean";

		public static string Convert(string clrTypeName)
		{
			if (clrTypeName.StartsWith("Int"))
				return YAMLTypes.Integer;

			switch(clrTypeName)
			{
				case "Decimal":
				case "Double":
				case "Float":
					return YAMLTypes.Number;
				case "String":
				case "DateTime":
				case "Stream":
					return YAMLTypes.String;
				case "Boolean":
					return YAMLTypes.Boolean;
			}

			return YAMLTypes.String;
		}
	}
}
