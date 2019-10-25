namespace TSSArt.StateMachine
{
	public interface IOnEntryBuilder
	{
		IOnEntry Build();

		void AddAction(IExecutableEntity action);
	}
}