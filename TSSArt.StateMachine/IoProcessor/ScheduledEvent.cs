namespace TSSArt.StateMachine
{
	public class ScheduledEvent
	{
		public ScheduledEvent(IOutgoingEvent @event) => Event = @event;

		public IOutgoingEvent Event { get; }

		public bool IsDisposed { get; private set; }

		public void Dispose() => IsDisposed = true;
	}
}