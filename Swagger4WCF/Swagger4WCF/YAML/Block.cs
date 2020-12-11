using System;

namespace Swagger4WCF.YAML
{
	public class Block : IDisposable
	{
		private Content m_Content;

		public Block(Content content)
		{
			this.m_Content = content;
			this.m_Content.Tabulation = new Tabulation(this.m_Content.Tabulation.Pattern, this.m_Content.Tabulation.Level + 1);
		}

		void IDisposable.Dispose()
		{
			this.m_Content.Tabulation = new Tabulation(this.m_Content.Tabulation.Pattern, this.m_Content.Tabulation.Level - 1);
		}
	}
}
