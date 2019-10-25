namespace TSSArt.StateMachine
{
	public interface IInitial : IEntity
	{
		ITransition Transition { get; }
	}
}