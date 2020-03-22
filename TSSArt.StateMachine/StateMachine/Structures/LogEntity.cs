namespace TSSArt.StateMachine
{
	public struct LogEntity : ILog, IVisitorEntity<LogEntity, ILog>, IAncestorProvider
	{
		public IValueExpression? Expression { get; set; }
		public string?           Label      { get; set; }

		void IVisitorEntity<LogEntity, ILog>.Init(ILog source)
		{
			Ancestor = source;
			Expression = source.Expression;
			Label = source.Label;
		}

		bool IVisitorEntity<LogEntity, ILog>.RefEquals(in LogEntity other) =>
				ReferenceEquals(Expression, other.Expression) &&
				ReferenceEquals(Label, other.Label);

		internal object? Ancestor;

		object? IAncestorProvider.Ancestor => Ancestor;
	}
}