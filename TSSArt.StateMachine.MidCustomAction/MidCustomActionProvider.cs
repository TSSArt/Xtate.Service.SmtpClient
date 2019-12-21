namespace TSSArt.StateMachine
{
	[CustomActionProvider("http://tssart.com/scxml/customaction/mid")]
	public class MidCustomActionProvider : CustomActionProviderBase
	{
		public static readonly ICustomActionProvider Instance = new MidCustomActionProvider();

		private MidCustomActionProvider()
		{
			Register(name: "storage", xmlReader => new StorageAction(xmlReader));
		}
	}
}