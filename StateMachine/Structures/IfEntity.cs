using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public struct IfEntity : IIf, IVisitorEntity<IfEntity, IIf>, IAncestorProvider
	{
		public ImmutableArray<IExecutableEntity> Action    { get; set; }
		public IConditionExpression?             Condition { get; set; }

		void IVisitorEntity<IfEntity, IIf>.Init(IIf source)
		{
			Ancestor = source;
			Action = source.Action;
			Condition = source.Condition!;
		}

		bool IVisitorEntity<IfEntity, IIf>.RefEquals(in IfEntity other) =>
				Action == other.Action &&
				ReferenceEquals(Condition, other.Condition);

		internal object? Ancestor;

		object? IAncestorProvider.Ancestor => Ancestor;
	}
}