namespace TSSArt.StateMachine
{
	public struct Cancel : ICancel, IEntity<Cancel, ICancel>, IAncestorProvider
	{
		public string           SendId;
		public IValueExpression SendIdExpression;

		string ICancel.SendId => SendId;

		IValueExpression ICancel.SendIdExpression => SendIdExpression;

		void IEntity<Cancel, ICancel>.Init(ICancel source)
		{
			Ancestor = source;
			SendId = source.SendId;
			SendIdExpression = source.SendIdExpression;
		}

		bool IEntity<Cancel, ICancel>.RefEquals(in Cancel other) =>
				ReferenceEquals(SendId, other.SendId) &&
				ReferenceEquals(SendIdExpression, other.SendIdExpression);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}