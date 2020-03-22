using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public struct TransitionEntity : ITransition, IVisitorEntity<TransitionEntity, ITransition>, IAncestorProvider
	{
		public ImmutableArray<IEventDescriptor>  EventDescriptors { get; set; }
		public IExecutableEntity?                Condition        { get; set; }
		public ImmutableArray<IIdentifier>       Target           { get; set; }
		public TransitionType                    Type             { get; set; }
		public ImmutableArray<IExecutableEntity> Action           { get; set; }

		void IVisitorEntity<TransitionEntity, ITransition>.Init(ITransition source)
		{
			Ancestor = source;
			Action = source.Action;
			Condition = source.Condition;
			EventDescriptors = source.EventDescriptors;
			Target = source.Target;
			Type = source.Type;
		}

		bool IVisitorEntity<TransitionEntity, ITransition>.RefEquals(in TransitionEntity other) =>
				Type == other.Type &&
				Target == other.Target &&
				Action == other.Action &&
				EventDescriptors == other.EventDescriptors &&
				ReferenceEquals(Condition, other.Condition);

		internal object? Ancestor;

		object? IAncestorProvider.Ancestor => Ancestor;
	}
}