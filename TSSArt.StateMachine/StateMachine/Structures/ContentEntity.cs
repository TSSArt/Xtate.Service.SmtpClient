namespace TSSArt.StateMachine
{
	public struct ContentEntity : IContent, IVisitorEntity<ContentEntity, IContent>, IAncestorProvider
	{
		public IValueExpression? Expression { get; set; }
		public IContentBody?     Body       { get; set; }

		void IVisitorEntity<ContentEntity, IContent>.Init(IContent source)
		{
			Ancestor = source;
			Expression = source.Expression;
			Body = source.Body;
		}

		bool IVisitorEntity<ContentEntity, IContent>.RefEquals(in ContentEntity other) =>
				ReferenceEquals(Expression, other.Expression) &&
				ReferenceEquals(Body, other.Body);

		internal object? Ancestor;

		object? IAncestorProvider.Ancestor => Ancestor;
	}
}