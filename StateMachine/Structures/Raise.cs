namespace TSSArt.StateMachine
{
	public struct Raise : IRaise, IEntity<Raise, IRaise>, IAncestorProvider
	{
		public IOutgoingEvent Event { get; set; }

		void IEntity<Raise, IRaise>.Init(IRaise source)
		{
			Ancestor = source;
			Event = source.Event;
		}

		bool IEntity<Raise, IRaise>.RefEquals(in Raise other) => ReferenceEquals(Event, other.Event);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}