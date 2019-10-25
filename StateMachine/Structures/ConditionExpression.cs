namespace TSSArt.StateMachine
{
	public struct ConditionExpression : IConditionExpression, IEntity<ConditionExpression, IConditionExpression>, IAncestorProvider
	{
		public string Expression;

		string IConditionExpression.Expression => Expression;

		void IEntity<ConditionExpression, IConditionExpression>.Init(IConditionExpression source)
		{
			Ancestor = source;
			Expression = source.Expression;
		}

		bool IEntity<ConditionExpression, IConditionExpression>.RefEquals(in ConditionExpression other) => ReferenceEquals(Expression, other.Expression);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}