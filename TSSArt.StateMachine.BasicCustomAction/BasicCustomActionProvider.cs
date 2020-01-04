namespace TSSArt.StateMachine
{
	[CustomActionProvider("http://tssart.com/scxml/customaction/basic")]
	public class BasicCustomActionProvider : CustomActionProviderBase
	{
		public static readonly ICustomActionProvider Instance = new BasicCustomActionProvider();

		private BasicCustomActionProvider()
		{
			Register(name: "base64decode", xmlReader => new Base64DecodeAction(xmlReader));
			Register(name: "parseUrl", xmlReader => new ParseUrlAction(xmlReader));
			Register(name: "format", xmlReader => new FormatAction(xmlReader));
			Register(name: "operation", xmlReader => new OperationAction(xmlReader));
		}
	}
}