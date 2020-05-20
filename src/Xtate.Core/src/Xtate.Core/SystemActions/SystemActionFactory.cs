using System;
using System.Xml;
using Xtate.Annotations;

namespace Xtate
{
	[PublicAPI]
	[CustomActionProvider("http://xtate.net/scxml/system")]
	public class SystemActionFactory : CustomActionFactoryBase
	{
		public static readonly ICustomActionFactory Instance = new SystemActionFactory();

		private SystemActionFactory()
		{
			Register(name: "start", (xmlReader, context) => new StartAction(xmlReader, context));
		}

		protected override void FillXmlNameTable(XmlNameTable xmlNameTable)
		{
			if (xmlNameTable == null) throw new ArgumentNullException(nameof(xmlNameTable));

			base.FillXmlNameTable(xmlNameTable);

			StartAction.FillXmlNameTable(xmlNameTable);
		}
	}
}