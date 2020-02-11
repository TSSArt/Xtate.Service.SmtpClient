using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public interface IEvent : IEntity
	{
		string                      SendId         { get; }
		ImmutableArray<IIdentifier> NameParts      { get; }
		EventType                   Type           { get; }
		Uri                         Origin         { get; }
		Uri                         OriginType     { get; }
		string                      InvokeId       { get; }
		string                      InvokeUniqueId { get; }
		DataModelValue              Data           { get; }
	}
}