using System;
using System.Xml;

namespace TSSArt.StateMachine
{
	[CustomActionProvider("http://tssart.com/scxml/customaction/mid")]
	public class MidCustomActionFactory : CustomActionFactoryBase
	{
		public static readonly ICustomActionFactory Instance = new MidCustomActionFactory();

		private MidCustomActionFactory()
		{
			Register(name: "storage", xmlReader => new StorageAction(xmlReader));
		}

		protected override void FillXmlNameTable(XmlNameTable xmlNameTable)
		{
			if (xmlNameTable == null) throw new ArgumentNullException(nameof(xmlNameTable));

			base.FillXmlNameTable(xmlNameTable);

			StorageAction.FillXmlNameTable(xmlNameTable);
		}
	}
}