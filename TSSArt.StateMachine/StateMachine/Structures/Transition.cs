using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public struct Transition : ITransition, IEntity<Transition, ITransition>, IAncestorProvider
	{
		public IReadOnlyList<IEventDescriptor>  Event     { get; set; }
		public IExecutableEntity                Condition { get; set; }
		public IReadOnlyList<IIdentifier>       Target    { get; set; }
		public TransitionType                   Type      { get; set; }
		public IReadOnlyList<IExecutableEntity> Action    { get; set; }

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