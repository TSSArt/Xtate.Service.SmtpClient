using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public interface IEvent : IEntity
	{
		IReadOnlyList<IIdentifier> NameParts { get; }
		EventType Type { get; }
		string SendId { get; }
		Uri Origin { get; }
		Uri OriginType { get; }
		string InvokeId { get; }
		DataModelValue Data { get; }
	}
}