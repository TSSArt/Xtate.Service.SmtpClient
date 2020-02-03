using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class EventNode : IOutgoingEvent, IStoreSupport, IAncestorProvider
	{
		private readonly IOutgoingEvent _event;

		public EventNode(IOutgoingEvent @event) => _event = @event ?? throw new ArgumentNullException(nameof(@event));

		object IAncestorProvider.Ancestor => _event;

		public ImmutableArray<IIdentifier> NameParts => _event.NameParts;
		public string                     SendId    => _event.SendId;
		public DataModelValue             Data      => _event.Data;
		public Uri                        Target    => _event.Target;
		public Uri                        Type      => _event.Type;
		public int                        DelayMs   => _event.DelayMs;

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.Id, EventName.ToName(_event.NameParts));
		}
	}
}