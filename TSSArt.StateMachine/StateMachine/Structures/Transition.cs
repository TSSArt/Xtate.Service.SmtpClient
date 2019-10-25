using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public struct Transition : ITransition, IEntity<Transition, ITransition>, IAncestorProvider
	{
		public IReadOnlyList<IEventDescriptor>  Event;
		public IExecutableEntity                Condition;
		public IReadOnlyList<IIdentifier>       Target;
		public TransitionType                   Type;
		public IReadOnlyList<IExecutableEntity> Action;

		IReadOnlyList<IEventDescriptor> ITransition.Event => Event;

		IExecutableEntity ITransition.Condition => Condition;

		IReadOnlyList<IIdentifier> ITransition.Target => Target;

		TransitionType ITransition.Type => Type;

		IReadOnlyList<IExecutableEntity> ITransition.Action => Action;

		void IEntity<Transition, ITransition>.Init(ITransition source)
		{
			Ancestor = source;
			Action = source.Action;
			Condition = source.Condition;
			Event = source.Event;
			Target = source.Target;
			Type = source.Type;
		}

		bool IEntity<Transition, ITransition>.RefEquals(in Transition other) =>
				Type == other.Type &&
				ReferenceEquals(Condition, other.Condition) &&
				ReferenceEquals(Target, other.Target) &&
				ReferenceEquals(Action, other.Action) &&
				ReferenceEquals(Event, other.Event);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}