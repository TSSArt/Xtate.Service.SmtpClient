namespace TSSArt.StateMachine
{
	public interface IRaise : IExecutableEntity
	{
		IEvent Event { get; }
	}
}