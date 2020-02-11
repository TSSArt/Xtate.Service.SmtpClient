using System;

namespace TSSArt.StateMachine
{
	internal sealed class EventDescriptorNode : IEventDescriptor, IStoreSupport, IAncestorProvider, IDebugEntityId
	{
		private readonly IEventDescriptor _eventDescriptor;

		public EventDescriptorNode(IEventDescriptor eventDescriptor) => _eventDescriptor = eventDescriptor ?? throw new ArgumentNullException(nameof(eventDescriptor));

		object IAncestorProvider.Ancestor => _eventDescriptor;

		FormattableString IDebugEntityId.EntityId => $"{_eventDescriptor}";

		public bool IsEventMatch(IEvent @event) => _eventDescriptor.IsEventMatch(@event);

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.EventDescriptorNode);
			bucket.Add(Key.Id, _eventDescriptor.ToString());
		}
	}
}