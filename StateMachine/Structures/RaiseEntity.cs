namespace TSSArt.StateMachine
{
	public struct RaiseEntity : IRaise, IVisitorEntity<RaiseEntity, IRaise>, IAncestorProvider
	{
		public IOutgoingEvent? OutgoingEvent { get; set; }

		void IVisitorEntity<RaiseEntity, IRaise>.Init(IRaise source)
		{
			Ancestor = source;
			OutgoingEvent = source.OutgoingEvent;
		}

		bool IVisitorEntity<RaiseEntity, IRaise>.RefEquals(in RaiseEntity other) => ReferenceEquals(OutgoingEvent, other.OutgoingEvent);

		internal object? Ancestor;

		object? IAncestorProvider.Ancestor => Ancestor;
	}
}