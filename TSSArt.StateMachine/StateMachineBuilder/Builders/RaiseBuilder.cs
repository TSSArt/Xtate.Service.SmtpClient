using System;

namespace TSSArt.StateMachine
{
	public class RaiseBuilder : IRaiseBuilder
	{
		private IOutgoingEvent _event;

		public IRaise Build()
		{
			if (_event == null)
			{
				throw new InvalidOperationException(message: "Event property required for Raise element");
			}

			return new Raise { Event = _event };
		}

		public void SetEvent(IOutgoingEvent @event) => _event = @event ?? throw new ArgumentNullException(nameof(@event));
	}
}