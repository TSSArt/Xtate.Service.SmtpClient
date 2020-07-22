namespace Xtate.Services
{
	[CustomActionProvider("http://xtate.net/scxml/customaction/mime")]
	public class MimeCustomActionFactory : CustomActionFactoryBase
	{
		public static readonly ICustomActionFactory Instance = new MimeCustomActionFactory();

		private MimeCustomActionFactory()
		{
			Register(name: "parseEmail", (xmlReader, context) => new ParseEmail(xmlReader, context));
		}
	}
}