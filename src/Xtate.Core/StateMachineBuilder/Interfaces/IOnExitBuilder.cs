namespace TSSArt.StateMachine
{
	public interface IOnExitBuilder
	{
		IOnExit Build();

		void AddAction(IExecutableEntity action);
	}
}