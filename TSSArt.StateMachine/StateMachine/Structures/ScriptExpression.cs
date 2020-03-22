namespace TSSArt.StateMachine
{
	public struct ScriptExpression : IScriptExpression, IVisitorEntity<ScriptExpression, IScriptExpression>, IAncestorProvider
	{
		public string? Expression { get; set; }

		void IVisitorEntity<ScriptExpression, IScriptExpression>.Init(IScriptExpression source)
		{
			Ancestor = source;
			Expression = source.Expression;
		}

		bool IVisitorEntity<ScriptExpression, IScriptExpression>.RefEquals(in ScriptExpression other) => ReferenceEquals(Expression, other.Expression);

		internal object? Ancestor;

		object? IAncestorProvider.Ancestor => Ancestor;
	}
}