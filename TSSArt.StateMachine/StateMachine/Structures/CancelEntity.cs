namespace TSSArt.StateMachine
{
	public struct CancelEntity : ICancel, IVisitorEntity<CancelEntity, ICancel>, IAncestorProvider
	{
		public string?           SendId           { get; set; }
		public IValueExpression? SendIdExpression { get; set; }

		void IVisitorEntity<CancelEntity, ICancel>.Init(ICancel source)
		{
			Ancestor = source;
			SendId = source.SendId;
			SendIdExpression = source.SendIdExpression;
		}

		bool IVisitorEntity<CancelEntity, ICancel>.RefEquals(in CancelEntity other) =>
				ReferenceEquals(SendId, other.SendId) &&
				ReferenceEquals(SendIdExpression, other.SendIdExpression);

		internal object? Ancestor;

		object? IAncestorProvider.Ancestor => Ancestor;
	}
}