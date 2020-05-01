using System;

namespace TSSArt.StateMachine
{
	internal sealed class EventDescriptorNode : IEventDescriptor, IStoreSupport, IAncestorProvider, IDebugEntityId
	{
		private readonly IEventDescriptor _eventDescriptor;

		public EventDescriptorNode(IEventDescriptor eventDescriptor)
		{
			_eventDescriptor = eventDescriptor;
		}

	#region Interface IAncestorProvider

		object? IAncestorProvider.Ancestor => _eventDescriptor;

	#endregion

	#region Interface IDebugEntityId

		FormattableString IDebugEntityId.EntityId => @$"{_eventDescriptor}";

	#endregion

	#region Interface IEventDescriptor

		public bool IsEventMatch(IEvent evt) => _eventDescriptor.IsEventMatch(evt);

	#endregion

	#region Interface IStoreSupport

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.EventDescriptorNode);
			bucket.Add(Key.Id, _eventDescriptor.As<string>());
		}

	#endregion
	}
}