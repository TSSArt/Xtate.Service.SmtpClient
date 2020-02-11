namespace TSSArt.StateMachine
{
	internal sealed class ConditionExpressionNode : IConditionExpression, IStoreSupport, IAncestorProvider
	{
		private readonly ConditionExpression _conditionExpression;

		public ConditionExpressionNode(in ConditionExpression conditionExpression) => _conditionExpression = conditionExpression;

		object IAncestorProvider.Ancestor => _conditionExpression.Ancestor;

		public string Expression => _conditionExpression.Expression;

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.ConditionExpressionNode);
			bucket.Add(Key.Expression, Expression);
		}
	}
}