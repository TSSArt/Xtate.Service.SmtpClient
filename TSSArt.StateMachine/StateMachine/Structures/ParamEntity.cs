namespace TSSArt.StateMachine
{
	public struct ParamEntity : IParam, IVisitorEntity<ParamEntity, IParam>, IAncestorProvider
	{
		public IValueExpression?    Expression { get; set; }
		public ILocationExpression? Location   { get; set; }
		public string?              Name       { get; set; }

		void IVisitorEntity<ParamEntity, IParam>.Init(IParam source)
		{
			Ancestor = source;
			Expression = source.Expression;
			Location = source.Location;
			Name = source.Name;
		}

		bool IVisitorEntity<ParamEntity, IParam>.RefEquals(in ParamEntity other) =>
				ReferenceEquals(Expression, other.Expression) &&
				ReferenceEquals(Location, other.Location) &&
				ReferenceEquals(Name, other.Name);

		internal object? Ancestor;

		object? IAncestorProvider.Ancestor => Ancestor;
	}
}