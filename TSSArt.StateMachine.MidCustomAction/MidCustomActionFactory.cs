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
	}
}