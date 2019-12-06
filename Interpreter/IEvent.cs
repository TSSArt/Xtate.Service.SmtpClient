using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public interface IEvent : IEntity
	{
		string                     SendId         { get; }
		IReadOnlyList<IIdentifier> NameParts      { get; }
		EventType                  Type           { get; }
		Uri                        Origin         { get; }
		Uri                        OriginType     { get; }
		string                     InvokeId       { get; }
		string                     InvokeUniqueId { get; }
		DataModelValue             Data           { get; }
	}
}