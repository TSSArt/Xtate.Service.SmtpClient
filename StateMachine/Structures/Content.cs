namespace TSSArt.StateMachine
{
	public struct Content : IContent, IVisitorEntity<Content, IContent>, IAncestorProvider
	{
		public IValueExpression Expression { get; set; }

		public IContentBody Body { get; set; }

		void IVisitorEntity<Content, IContent>.Init(IContent source)
		{
			Ancestor = source;
			Expression = source.Expression;
			Body = source.Body;
		}

		bool IVisitorEntity<Content, IContent>.RefEquals(in Content other) =>
				ReferenceEquals(Expression, other.Expression) &&
				ReferenceEquals(Body, other.Body);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}