using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public class EventObject : IEvent, IStoreSupport
	{
		private static readonly char[] Dot = { '.' };
		private readonly        string _name;

		public EventObject(EventType type, string name, DataModelValue data = default) : this(type, sendId: null, name, invokeId: null, origin: null, originType: null, data) { }

		public EventObject(EventType type, string sendId, string name, DataModelValue data = default) : this(type, sendId, name, invokeId: null, origin: null, originType: null, data) { }

		public EventObject(EventType type, string sendId, string name, string invokeId, Uri origin, Uri originType, DataModelValue data)
		{
			if (string.IsNullOrEmpty(name)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(name));

			_name = name;

			Type = type;
			SendId = sendId;
			InvokeId = invokeId;
			Origin = origin;
			OriginType = originType;
			Data = data;

			NameParts = IdentifierList.Create(name.Split(Dot, StringSplitOptions.None), p => (Identifier) p);
		}

		public EventObject(in Bucket bucket)
		{
			if (!bucket.TryGet(Key.TypeInfo, out TypeInfo storedTypeInfo) || storedTypeInfo != TypeInfo.EventObject)
			{
				throw new ArgumentException("Invalid TypeInfo value");
			}

			_name = bucket.GetString(Key.Name);
			NameParts = IdentifierList.Create(_name.Split(Dot, StringSplitOptions.None), p => (Identifier) p);
			Type = bucket.Get<EventType>(Key.Type);
			SendId = bucket.GetString(Key.SendId);
			Origin = bucket.GetUri(Key.Origin);
			OriginType = bucket.GetUri(Key.OriginType);
			InvokeId = bucket.GetString(Key.InvokeId);

			if (bucket.GetBoolean(Key.Data))
			{
				using var tracker = new DataModelReferenceTracker(bucket.Nested(Key.DataReferences));
				Data = bucket.Nested(Key.DataValue).GetDataModelValue(tracker, baseValue: default);
			}
		}

		public DataModelValue Data { get; }

		public string InvokeId { get; }

		public IReadOnlyList<IIdentifier> NameParts { get; }

		public Uri Origin { get; }

		public Uri OriginType { get; }

		public string SendId { get; }

		public EventType Type { get; }

		public void Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.EventObject);
			bucket.Add(Key.Name, _name);
			bucket.Add(Key.Type, Type);
			bucket.Add(Key.SendId, SendId);
			bucket.Add(Key.Origin, Origin);
			bucket.Add(Key.OriginType, OriginType);
			bucket.Add(Key.InvokeId, InvokeId);

			if (Data.Type != DataModelValueType.Undefined)
			{
				bucket.Add(Key.Data, value: true);
				using var tracker = new DataModelReferenceTracker(bucket.Nested(Key.DataReferences));
				bucket.Nested(Key.DataValue).SetDataModelValue(tracker, Data);
			}
		}

		public override string ToString() => _name;
	}
}