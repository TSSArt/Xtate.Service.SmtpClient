namespace TSSArt.StateMachine
{
	public interface IEventDescriptor : IEntity
	{
		bool IsEventMatch(IEvent evt);
	}
}