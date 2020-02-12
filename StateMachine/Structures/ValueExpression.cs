namespace TSSArt.StateMachine
{
	public struct ValueExpression : IValueExpression, IVisitorEntity<ValueExpression, IValueExpression>, IAncestorProvider
	{
		public string Expression { get; set; }

		void IVisitorEntity<ValueExpression, IValueExpression>.Init(IValueExpression source)
		{
			Ancestor = source;
			Expression = source.Expression;
		}

		bool IVisitorEntity<ValueExpression, IValueExpression>.RefEquals(in ValueExpression other) => ReferenceEquals(Expression, other.Expression);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}