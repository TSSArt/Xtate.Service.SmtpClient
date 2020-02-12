namespace TSSArt.StateMachine
{
	public struct Cancel : ICancel, IVisitorEntity<Cancel, ICancel>, IAncestorProvider
	{
		public string           SendId           { get; set; }
		public IValueExpression SendIdExpression { get; set; }

		void IVisitorEntity<Cancel, ICancel>.Init(ICancel source)
		{
			Ancestor = source;
			SendId = source.SendId;
			SendIdExpression = source.SendIdExpression;
		}

		bool IVisitorEntity<Cancel, ICancel>.RefEquals(in Cancel other) =>
				ReferenceEquals(SendId, other.SendId) &&
				ReferenceEquals(SendIdExpression, other.SendIdExpression);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}