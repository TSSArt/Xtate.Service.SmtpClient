using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public interface ITransition : IEntity
	{
		IReadOnlyList<IEventDescriptor>  Event     { get; }
		IExecutableEntity                Condition { get; }
		IReadOnlyList<IIdentifier>       Target    { get; }
		TransitionType                   Type      { get; }
		IReadOnlyList<IExecutableEntity> Action    { get; }
	}
}