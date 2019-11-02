namespace TSSArt.StateMachine
{
	public interface IRaise : IExecutableEntity
	{
		IOutgoingEvent Event { get; }
	}
}