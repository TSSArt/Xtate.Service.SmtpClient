namespace Xtate
{
	public interface ICustomActionFactory
	{
		bool CanHandle(string ns, string name);

		ICustomActionExecutor CreateExecutor(ICustomActionContext customActionContext);
	}
}