namespace TSSArt.StateMachine
{
	public struct ValueExpression : IValueExpression, IEntity<ValueExpression, IValueExpression>, IAncestorProvider
	{
		public string Expression;

		string IValueExpression.Expression => Expression;

		void IEntity<ValueExpression, IValueExpression>.Init(IValueExpression source)
		{
			Ancestor = source;
			Expression = source.Expression;
		}

		bool IEntity<ValueExpression, IValueExpression>.RefEquals(in ValueExpression other) => ReferenceEquals(Expression, other.Expression);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}