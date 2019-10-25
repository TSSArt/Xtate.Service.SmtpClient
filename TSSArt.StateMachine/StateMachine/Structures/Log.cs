namespace TSSArt.StateMachine
{
	public struct Log : ILog, IEntity<Log, ILog>, IAncestorProvider
	{
		public IValueExpression Expression;
		public string           Label;

		IValueExpression ILog.Expression => Expression;

		string ILog.Label => Label;

		void IEntity<Log, ILog>.Init(ILog source)
		{
			Ancestor = source;
			Expression = source.Expression;
			Label = source.Label;
		}

		bool IEntity<Log, ILog>.RefEquals(in Log other) =>
				ReferenceEquals(Expression, other.Expression) &&
				ReferenceEquals(Label, other.Label);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}