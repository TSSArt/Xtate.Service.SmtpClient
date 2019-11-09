namespace TSSArt.StateMachine
{
	public struct Script : IScript, IEntity<Script, IScript>, IAncestorProvider
	{
		public IScriptExpression         Content { get; set; }
		public IExternalScriptExpression Source  { get; set; }

		void IEntity<Script, IScript>.Init(IScript source)
		{
			Ancestor = source;
			Content = source.Content;
			Source = source.Source;
		}

		bool IEntity<Script, IScript>.RefEquals(in Script other) =>
				ReferenceEquals(Content, other.Content) &&
				ReferenceEquals(Source, other.Source);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}