using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public struct Transition : ITransition, IEntity<Transition, ITransition>, IAncestorProvider
	{
		public ImmutableArray<IEventDescriptor>  Event     { get; set; }
		public IExecutableEntity                 Condition { get; set; }
		public ImmutableArray<IIdentifier>       Target    { get; set; }
		public TransitionType                    Type      { get; set; }
		public ImmutableArray<IExecutableEntity> Action    { get; set; }

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
				Target == other.Target &&
				Action == other.Action &&
				Event == other.Event &&
				ReferenceEquals(Condition, other.Condition);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}