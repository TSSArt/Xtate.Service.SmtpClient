namespace TSSArt.StateMachine
{
	public interface IFinalizeBuilder
	{
		IFinalize Build();

		void AddAction(IExecutableEntity action);
	}
}