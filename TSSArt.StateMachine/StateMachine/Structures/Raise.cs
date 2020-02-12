namespace TSSArt.StateMachine
{
	public struct Raise : IRaise, IVisitorEntity<Raise, IRaise>, IAncestorProvider
	{
		public IOutgoingEvent Event { get; set; }

		void IVisitorEntity<Raise, IRaise>.Init(IRaise source)
		{
			Ancestor = source;
			Event = source.Event;
		}

		bool IVisitorEntity<Raise, IRaise>.RefEquals(in Raise other) => ReferenceEquals(Event, other.Event);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}