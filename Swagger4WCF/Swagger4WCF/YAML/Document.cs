using Mono.Cecil;

namespace Swagger4WCF.YAML
{
	public class Document
	{
		static public Document Generate(TypeDefinition type, Documentation documentation, AssemblyDefinition assembly)
		{
			return Content.Generate(type, documentation ?? Documentation.Empty(), assembly);
		}

		static public implicit operator string(Document document)
		{
			return document?.ToString();
		}

		private TypeDefinition m_Type;
		private string m_Value;

		public Document(TypeDefinition type, string value)
		{
			this.m_Type = type;
			this.m_Value = value;
		}

		public TypeDefinition Type
		{
			get { return this.m_Type; }
		}

		override public string ToString()
		{
			return this.m_Value;
		}
	}
}
