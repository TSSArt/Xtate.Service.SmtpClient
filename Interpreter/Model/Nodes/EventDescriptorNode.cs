using System;

namespace TSSArt.StateMachine
{
	internal sealed class EventDescriptorNode : IEventDescriptor, IStoreSupport, IAncestorProvider, IDebugEntityId
	{
		private readonly IEventDescriptor _eventDescriptor;

		public EventDescriptorNode(IEventDescriptor eventDescriptor)
		{
			Infrastructure.Assert(eventDescriptor != null);

			_eventDescriptor = eventDescriptor;
		}

		object? IAncestorProvider.Ancestor => _eventDescriptor;

		FormattableString IDebugEntityId.EntityId => @$"{_eventDescriptor}";

		public bool IsEventMatch(IEvent evt) => _eventDescriptor.IsEventMatch(evt);

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.EventDescriptorNode);
			bucket.Add(Key.Id, _eventDescriptor.As<string>());
		}
	}
}