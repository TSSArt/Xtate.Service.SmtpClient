namespace TSSArt.StateMachine
{
	public interface ICustomActionFactory
	{
		bool CanHandle(string ns, string name);

		ICustomActionExecutor CreateExecutor(string xml);
	}
}