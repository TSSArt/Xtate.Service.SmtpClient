namespace TSSArt.StateMachine
{
	public struct Log : ILog, IVisitorEntity<Log, ILog>, IAncestorProvider
	{
		public IValueExpression Expression { get; set; }
		public string           Label      { get; set; }

		void IVisitorEntity<Log, ILog>.Init(ILog source)
		{
			Ancestor = source;
			Expression = source.Expression;
			Label = source.Label;
		}

		bool IVisitorEntity<Log, ILog>.RefEquals(in Log other) =>
				ReferenceEquals(Expression, other.Expression) &&
				ReferenceEquals(Label, other.Label);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}