using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public interface ITransitionBuilder
	{
		ITransition Build();

		void SetEvent(IReadOnlyList<IEventDescriptor> eventsDescriptor);
		void SetCondition(IExecutableEntity condition);
		void SetTarget(IReadOnlyList<IIdentifier> target);
		void SetType(TransitionType type);
		void AddAction(IExecutableEntity action);
	}
}