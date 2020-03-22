namespace TSSArt.StateMachine
{
	public struct ConditionExpression : IConditionExpression, IVisitorEntity<ConditionExpression, IConditionExpression>, IAncestorProvider
	{
		public string? Expression { get; set; }

		void IVisitorEntity<ConditionExpression, IConditionExpression>.Init(IConditionExpression source)
		{
			Ancestor = source;
			Expression = source.Expression;
		}

		bool IVisitorEntity<ConditionExpression, IConditionExpression>.RefEquals(in ConditionExpression other) => ReferenceEquals(Expression, other.Expression);

		internal object? Ancestor;

		object? IAncestorProvider.Ancestor => Ancestor;
	}
}