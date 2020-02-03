using System.Collections./**/Immutable;

namespace TSSArt.StateMachine
{
	public interface ITransition : IEntity
	{
		/**/ImmutableArray<IEventDescriptor>  Event     { get; }
		IExecutableEntity                Condition { get; }
		/**/ImmutableArray<IIdentifier>       Target    { get; }
		TransitionType                   Type      { get; }
		/**/ImmutableArray<IExecutableEntity> Action    { get; }
	}
}