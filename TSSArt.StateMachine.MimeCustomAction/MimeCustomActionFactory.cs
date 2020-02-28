namespace TSSArt.StateMachine.Services
{
	[CustomActionProvider("http://tssart.com/scxml/customaction/mime")]
	public class MimeCustomActionFactory : CustomActionFactoryBase
	{
		public static readonly ICustomActionFactory Instance = new MimeCustomActionFactory();

		private MimeCustomActionFactory()
		{
			Register(name: "parseEmail", xmlReader => new ParseEmail(xmlReader));
		}
	}
}