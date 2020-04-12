﻿using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	internal sealed class EventObject : IEvent, IStoreSupport
	{
		public EventObject(string name, string? invokeId = null, string? invokeUniqueId = null) :
				this(EventType.External, EventName.ToParts(name), data: default, sendId: null, invokeId, invokeUniqueId) { }

		public EventObject(EventType type, IOutgoingEvent evt, Uri? origin = null, Uri? originType = null, string? invokeId = null, string? invokeUniqueId = null)
				: this(type, evt.SendId, evt.NameParts, invokeId, invokeUniqueId, origin, originType, evt.Data) { }

		public EventObject(EventType type, ImmutableArray<IIdentifier> nameParts, DataModelValue data = default, string? sendId = null, string? invokeId = null, string? invokeUniqueId = null)
				: this(type, sendId, nameParts, invokeId, invokeUniqueId, origin: null, originType: null, data) { }

		private EventObject(EventType type, string? sendId, ImmutableArray<IIdentifier> nameParts, string? invokeId, string? invokeUniqueId, Uri? origin, Uri? originType, DataModelValue data)
		{
			Type = type;
			SendId = sendId;
			NameParts = nameParts;
			InvokeId = invokeId;
			InvokeUniqueId = invokeUniqueId;
			Origin = origin;
			OriginType = originType;
			Data = data.AsConstant();
		}

		public EventObject(in Bucket bucket)
		{
			if (!bucket.TryGet(Key.TypeInfo, out TypeInfo storedTypeInfo) || storedTypeInfo != TypeInfo.EventObject)
			{
				throw new ArgumentException(Resources.Exception_Invalid_TypeInfo_value);
			}

			var name = bucket.GetString(Key.Name);
			NameParts = name != null ? EventName.ToParts(name) : default;
			Type = bucket.Get<EventType>(Key.Type);
			SendId = bucket.GetString(Key.SendId);
			Origin = bucket.GetUri(Key.Origin);
			OriginType = bucket.GetUri(Key.OriginType);
			InvokeId = bucket.GetString(Key.InvokeId);

			if (bucket.TryGet(Key.Data, out bool data) && data)
			{
				using var tracker = new DataModelReferenceTracker(bucket.Nested(Key.DataReferences));
				Data = bucket.Nested(Key.DataValue).GetDataModelValue(tracker, baseValue: default).AsConstant();
			}
		}

	#region Interface IEvent

		public DataModelValue Data { get; }

		public string? InvokeId { get; }

		public string? InvokeUniqueId { get; }

		public ImmutableArray<IIdentifier> NameParts { get; }

		public Uri? Origin { get; }

		public Uri? OriginType { get; }

		public string? SendId { get; }

		public EventType Type { get; }

	#endregion

	#region Interface IStoreSupport

		public void Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.EventObject);
			bucket.Add(Key.Name, EventName.ToName(NameParts));
			bucket.Add(Key.Type, Type);
			bucket.Add(Key.SendId, SendId);
			bucket.Add(Key.Origin, Origin);
			bucket.Add(Key.OriginType, OriginType);
			bucket.Add(Key.InvokeId, InvokeId);

			if (!Data.IsUndefinedOrNull())
			{
				bucket.Add(Key.Data, value: true);
				using var tracker = new DataModelReferenceTracker(bucket.Nested(Key.DataReferences));
				bucket.Nested(Key.DataValue).SetDataModelValue(tracker, Data);
			}
		}

	#endregion
	}
}