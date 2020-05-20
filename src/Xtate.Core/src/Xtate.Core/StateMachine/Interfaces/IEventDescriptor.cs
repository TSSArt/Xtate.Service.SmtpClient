namespace TSSArt.StateMachine
{
	public interface IEventDescriptor : IEntity
	{
		string Value { get; }

		bool IsEventMatch(IEvent evt);
	}
}