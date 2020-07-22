namespace Xtate
{
	[CustomActionProvider("http://xtate.net/scxml/customaction/basic")]
	public class BasicCustomActionFactory : CustomActionFactoryBase
	{
		public static readonly ICustomActionFactory Instance = new BasicCustomActionFactory();

		private BasicCustomActionFactory()
		{
			Register(name: "base64decode", (xmlReader, context) => new Base64DecodeAction(xmlReader, context));
			Register(name: "parseUrl", (xmlReader, context) => new ParseUrlAction(xmlReader, context));
			Register(name: "format", (xmlReader, context) => new FormatAction(xmlReader, context));
			Register(name: "operation", (xmlReader, context) => new OperationAction(xmlReader, context));
		}
	}
}