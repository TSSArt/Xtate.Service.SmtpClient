using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	internal class EventObject : IEvent
	{
		public EventObject(string eventName, Uri origin, Uri originType, DataModelValue data)
		{
			NameParts = EventName.ToParts(eventName);
			Origin = origin;
			OriginType = originType;
			Data = data.AsConstant();
		}

	#region Interface IEvent

		public DataModelValue Data { get; }

		public string? InvokeId => null;

		public string? InvokeUniqueId => null;

		public ImmutableArray<IIdentifier> NameParts { get; }

		public Uri Origin { get; }

		public Uri OriginType { get; }

		public string? SendId => null;

		public EventType Type => EventType.External;

	#endregion
	}
}