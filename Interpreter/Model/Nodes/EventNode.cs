using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	internal sealed class EventNode : IOutgoingEvent, IStoreSupport, IAncestorProvider
	{
		private readonly IOutgoingEvent _event;

		public EventNode(IOutgoingEvent evt)
		{
			Infrastructure.Assert(evt != null);

			_event = evt;
		}

		object? IAncestorProvider.Ancestor => _event;

		public ImmutableArray<IIdentifier> NameParts => _event.NameParts;
		public string?                     SendId    => _event.SendId;
		public DataModelValue              Data      => _event.Data;
		public Uri?                        Target    => _event.Target;
		public Uri?                        Type      => _event.Type;
		public int                         DelayMs   => _event.DelayMs;

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.Id, EventName.ToName(_event.NameParts));
		}
	}
}