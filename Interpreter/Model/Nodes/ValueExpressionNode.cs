namespace TSSArt.StateMachine
{
	public class ValueExpressionNode : IValueExpression, IStoreSupport, IAncestorProvider
	{
		private readonly ValueExpression _valueExpression;

		public ValueExpressionNode(in ValueExpression valueExpression) => _valueExpression = valueExpression;

		object IAncestorProvider.Ancestor => _valueExpression.Ancestor;

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.ValueExpressionNode);
			bucket.Add(Key.Expression, Expression);
		}

		public string Expression => _valueExpression.Expression;
	}
}