namespace TSSArt.StateMachine
{
	public struct ScriptEntity : IScript, IVisitorEntity<ScriptEntity, IScript>, IAncestorProvider
	{
		public IScriptExpression?         Content { get; set; }
		public IExternalScriptExpression? Source  { get; set; }

		void IVisitorEntity<ScriptEntity, IScript>.Init(IScript source)
		{
			Ancestor = source;
			Content = source.Content;
			Source = source.Source;
		}

		bool IVisitorEntity<ScriptEntity, IScript>.RefEquals(in ScriptEntity other) =>
				ReferenceEquals(Content, other.Content) &&
				ReferenceEquals(Source, other.Source);

		internal object? Ancestor;

		object? IAncestorProvider.Ancestor => Ancestor;
	}
}