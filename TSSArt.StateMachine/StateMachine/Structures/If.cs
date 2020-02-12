using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public struct If : IIf, IVisitorEntity<If, IIf>, IAncestorProvider
	{
		public ImmutableArray<IExecutableEntity> Action    { get; set; }
		public IConditionExpression              Condition { get; set; }

		void IVisitorEntity<If, IIf>.Init(IIf source)
		{
			Ancestor = source;
			Action = source.Action;
			Condition = source.Condition;
		}

		bool IVisitorEntity<If, IIf>.RefEquals(in If other) =>
				Action == other.Action &&
				ReferenceEquals(Condition, other.Condition);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}