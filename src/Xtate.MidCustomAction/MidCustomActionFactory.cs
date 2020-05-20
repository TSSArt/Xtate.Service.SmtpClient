using System;
using System.Xml;
using Xtate.Annotations;

namespace Xtate
{
	[PublicAPI]
	[CustomActionProvider("http://xtate.net/scxml/customaction/mid")]
	public class MidCustomActionFactory : CustomActionFactoryBase
	{
		public static readonly ICustomActionFactory Instance = new MidCustomActionFactory();

		private MidCustomActionFactory()
		{
			Register(name: "storage", (xmlReader, context) => new StorageAction(xmlReader, context));
		}

		protected override void FillXmlNameTable(XmlNameTable xmlNameTable)
		{
			if (xmlNameTable == null) throw new ArgumentNullException(nameof(xmlNameTable));

			base.FillXmlNameTable(xmlNameTable);

			StorageAction.FillXmlNameTable(xmlNameTable);
		}
	}
}