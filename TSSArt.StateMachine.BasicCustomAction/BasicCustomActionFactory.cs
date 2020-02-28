namespace TSSArt.StateMachine
{
	[CustomActionProvider("http://tssart.com/scxml/customaction/basic")]
	public class BasicCustomActionFactory : CustomActionFactoryBase
	{
		public static readonly ICustomActionFactory Instance = new BasicCustomActionFactory();

		private BasicCustomActionFactory()
		{
			Register(name: "base64decode", xmlReader => new Base64DecodeAction(xmlReader));
			Register(name: "parseUrl", xmlReader => new ParseUrlAction(xmlReader));
			Register(name: "format", xmlReader => new FormatAction(xmlReader));
			Register(name: "operation", xmlReader => new OperationAction(xmlReader));
		}
	}
}