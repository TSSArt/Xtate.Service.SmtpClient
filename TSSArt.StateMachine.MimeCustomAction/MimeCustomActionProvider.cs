namespace TSSArt.StateMachine.Services
{
	[CustomActionProvider("http://tssart.com/scxml/customaction/mime")]
	public class MimeCustomActionProvider : CustomActionProviderBase
	{
		public static readonly ICustomActionProvider Instance = new MimeCustomActionProvider();

		private MimeCustomActionProvider()
		{
			Register(name: "parseEmail", xmlReader => new ParseEmail(xmlReader));
		}
	}
}