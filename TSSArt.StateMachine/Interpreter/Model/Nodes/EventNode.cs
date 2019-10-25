using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public class EventNode : IEvent, IStoreSupport, IAncestorProvider, IDebugEntityId
	{
		private readonly IEvent _event;

		public EventNode(IEvent @event)
		{
			_event = @event ?? throw new ArgumentNullException(nameof(@event));

			if (@event.Type != EventType.Internal || @event.Origin != null || @event.OriginType != null || 
				@event.InvokeId != null || @event.SendId != null || @event.Data.Type != DataModelValueType.Undefined)
			{
				throw new ArgumentException("Allowed only named internal event");
			}
		}

		object IAncestorProvider.Ancestor => _event;

		FormattableString IDebugEntityId.EntityId => $"{_event}";

		public IReadOnlyList<IIdentifier> NameParts  => _event.NameParts;
		public EventType                  Type       => _event.Type;
		public string                     SendId     => _event.SendId;
		public Uri                        Origin     => _event.Origin;
		public Uri                        OriginType => _event.OriginType;
		public string                     InvokeId   => _event.InvokeId;
		public DataModelValue             Data       => _event.Data;

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.Id, _event.ToString());
		}
	}
}