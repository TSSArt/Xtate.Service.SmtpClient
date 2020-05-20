using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public struct IfEntity : IIf, IVisitorEntity<IfEntity, IIf>, IAncestorProvider
	{
		internal object? Ancestor;

	#region Interface IAncestorProvider

		object? IAncestorProvider.Ancestor => Ancestor;

	#endregion

	#region Interface IIf

		public ImmutableArray<IExecutableEntity> Action    { get; set; }
		public IConditionExpression?             Condition { get; set; }

	#endregion

	#region Interface IVisitorEntity<IfEntity,IIf>

		void IVisitorEntity<IfEntity, IIf>.Init(IIf source)
		{
			Ancestor = source;
			Action = source.Action;
			Condition = source.Condition!;
		}

		bool IVisitorEntity<IfEntity, IIf>.RefEquals(in IfEntity other) =>
				Action == other.Action &&
				ReferenceEquals(Condition, other.Condition);

	#endregion
	}
}