using System;

namespace TSSArt.StateMachine
{
	public class ScheduledEvent
	{
		private readonly int      _delayMs;
		private readonly DateTime _fireOnUtc;

		public ScheduledEvent(IEvent @event, Uri type, Uri target, int delayMs)
		{
			Event = @event;
			Type = type;
			Target = target;
			_delayMs = delayMs;
			_fireOnUtc = DateTime.UtcNow.AddMilliseconds(delayMs);
		}

		public IEvent Event  { get; }
		public Uri    Type   { get; }
		public Uri    Target { get; }

		public string SendId => Event.SendId;

		public bool IsDisposed { get; private set; }

		public void Dispose() => IsDisposed = true;
	}
}