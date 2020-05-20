namespace TSSArt.StateMachine
{
	public interface IRaise : IExecutableEntity
	{
		IOutgoingEvent? OutgoingEvent { get; }
	}
}