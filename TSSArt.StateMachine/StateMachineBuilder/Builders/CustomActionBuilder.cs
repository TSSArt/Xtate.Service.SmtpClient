using System;

namespace TSSArt.StateMachine
{
	public class CustomActionBuilder : ICustomActionBuilder
	{
		private string _xml;

		public ICustomAction Build()
		{
			if (_xml == null)
			{
				throw new InvalidOperationException(message: "Xml cannot be null");
			}

			return new CustomAction { Xml = _xml };
		}

		public void SetXml(string xml)
		{
			_xml = xml ?? throw new ArgumentNullException(nameof(xml));
		}
	}
}