namespace TSSArt.StateMachine
{
	internal sealed class ScriptExpressionNode : IScriptExpression, IStoreSupport, IAncestorProvider
	{
		private readonly ScriptExpression _scriptExpression;

		public ScriptExpressionNode(in ScriptExpression scriptExpression)
		{
			Infrastructure.Assert(scriptExpression.Expression != null);

			_scriptExpression = scriptExpression;
		}

		object? IAncestorProvider.Ancestor => _scriptExpression.Ancestor;

		public string Expression => _scriptExpression.Expression!;

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.ScriptExpressionNode);
			bucket.Add(Key.Expression, Expression);
		}
	}
}