namespace TSSArt.StateMachine
{
	public interface IInitialBuilder
	{
		IInitial Build();

		void SetTransition(ITransition transition);
	}
}