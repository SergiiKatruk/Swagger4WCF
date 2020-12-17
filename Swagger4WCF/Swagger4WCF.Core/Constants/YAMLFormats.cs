namespace Swagger4WCF.Core.Constants
{
	class YAMLFormats
	{
		public const string Double = "double";
		public const string Float = "float";
		public const string Binary = "binary";
		public const string Date = "date";
		public const string DateTime = "date-time";
		public const string Password = "password";

		public static string Convert(string clrTypeName)
		{
			if (clrTypeName.StartsWith("Int"))
				return clrTypeName;

			switch (clrTypeName)
			{
				case "Decimal":
				case "Double":
					return YAMLFormats.Double;
				case "Float":
					return YAMLFormats.Float;
				case "DateTime":
					return YAMLFormats.Date;
				case "Stream":
					return YAMLFormats.Binary;
			}

			return string.Empty;
		}
	}
}
