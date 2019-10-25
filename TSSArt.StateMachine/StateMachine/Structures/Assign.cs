namespace TSSArt.StateMachine
{
	public struct Assign : IAssign, IEntity<Assign, IAssign>, IAncestorProvider
	{
		public ILocationExpression Location;
		public IValueExpression    Expression;
		public string              InlineContent;

		ILocationExpression IAssign.Location => Location;

		IValueExpression IAssign.Expression => Expression;

		string IAssign.InlineContent => InlineContent;

		void IEntity<Assign, IAssign>.Init(IAssign source)
		{
			Ancestor = source;
			Location = source.Location;
			InlineContent = source.InlineContent;
			Expression = source.Expression;
		}

		bool IEntity<Assign, IAssign>.RefEquals(in Assign other) =>
				ReferenceEquals(Location, other.Location) &&
				ReferenceEquals(Expression, other.Expression) &&
				ReferenceEquals(InlineContent, other.InlineContent);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}