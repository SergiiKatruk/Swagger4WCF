using Swagger4WCF.Core.Interfaces;
using System;

namespace Swagger4WCF.Core.YAML
{
	public class Block : IDisposable
	{
		private IYAMLContent m_Content;

		public Block(IYAMLContent content)
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
