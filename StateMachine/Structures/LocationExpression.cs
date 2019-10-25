namespace TSSArt.StateMachine
{
	public struct LocationExpression : ILocationExpression, IEntity<LocationExpression, ILocationExpression>, IAncestorProvider
	{
		public string Expression;

		string ILocationExpression.Expression => Expression;

		void IEntity<LocationExpression, ILocationExpression>.Init(ILocationExpression source)
		{
			Ancestor = source;
			Expression = source.Expression;
		}

		bool IEntity<LocationExpression, ILocationExpression>.RefEquals(in LocationExpression other) => ReferenceEquals(Expression, other.Expression);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}