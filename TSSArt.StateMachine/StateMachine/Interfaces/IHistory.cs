namespace TSSArt.StateMachine
{
	public interface IHistory : IEntity
	{
		IIdentifier Id         { get; }
		HistoryType Type       { get; }
		ITransition Transition { get; }
	}
}