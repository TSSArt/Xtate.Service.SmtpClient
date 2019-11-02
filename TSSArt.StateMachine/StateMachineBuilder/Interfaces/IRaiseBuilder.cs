namespace TSSArt.StateMachine
{
	public interface IRaiseBuilder
	{
		IRaise Build();

		void SetEvent(IOutgoingEvent @event);
	}
}