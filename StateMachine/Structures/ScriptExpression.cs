namespace TSSArt.StateMachine
{
	public struct ScriptExpression : IScriptExpression, IEntity<ScriptExpression, IScriptExpression>, IAncestorProvider
	{
		public string Expression;

		string IScriptExpression.Expression => Expression;

		void IEntity<ScriptExpression, IScriptExpression>.Init(IScriptExpression source)
		{
			Ancestor = source;
			Expression = source.Expression;
		}

		bool IEntity<ScriptExpression, IScriptExpression>.RefEquals(in ScriptExpression other) => ReferenceEquals(Expression, other.Expression);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}