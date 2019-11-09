namespace TSSArt.StateMachine
{
	public struct Param : IParam, IEntity<Param, IParam>, IAncestorProvider
	{
		public IValueExpression    Expression { get; set; }
		public ILocationExpression Location   { get; set; }
		public string              Name       { get; set; }

		void IEntity<Param, IParam>.Init(IParam source)
		{
			Ancestor = source;
			Expression = source.Expression;
			Location = source.Location;
			Name = source.Name;
		}

		bool IEntity<Param, IParam>.RefEquals(in Param other) =>
				ReferenceEquals(Expression, other.Expression) &&
				ReferenceEquals(Location, other.Location) &&
				ReferenceEquals(Name, other.Name);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}