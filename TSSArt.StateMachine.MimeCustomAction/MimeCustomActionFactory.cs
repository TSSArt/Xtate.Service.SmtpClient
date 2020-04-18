using System;
using System.Xml;

namespace TSSArt.StateMachine.Services
{
	[CustomActionProvider("http://tssart.com/scxml/customaction/mime")]
	public class MimeCustomActionFactory : CustomActionFactoryBase
	{
		public static readonly ICustomActionFactory Instance = new MimeCustomActionFactory();

		private MimeCustomActionFactory()
		{
			Register(name: "parseEmail", (xmlReader, context) => new ParseEmail(xmlReader, context));
		}

		protected override void FillXmlNameTable(XmlNameTable xmlNameTable)
		{
			if (xmlNameTable == null) throw new ArgumentNullException(nameof(xmlNameTable));

			base.FillXmlNameTable(xmlNameTable);

			ParseEmail.FillXmlNameTable(xmlNameTable);
		}
	}
}