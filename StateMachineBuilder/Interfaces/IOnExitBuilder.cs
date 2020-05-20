namespace Xtate
{
	public interface IOnExitBuilder
	{
		IOnExit Build();

		void AddAction(IExecutableEntity action);
	}
}