using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public interface IOutgoingEvent : IEntity
	{
		string                     SendId    { get; }
		IReadOnlyList<IIdentifier> NameParts { get; }
		Uri                        Target    { get; }
		Uri                        Type      { get; }
		int                        DelayMs   { get; }
		DataModelValue             Data      { get; }
	}
}