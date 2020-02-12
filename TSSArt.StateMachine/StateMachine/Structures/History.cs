using System;

namespace TSSArt.StateMachine
{
	public struct History : IHistory, IVisitorEntity<History, IHistory>, IAncestorProvider, IDebugEntityId
	{
		public IIdentifier Id         { get; set; }
		public HistoryType Type       { get; set; }
		public ITransition Transition { get; set; }

		void IVisitorEntity<History, IHistory>.Init(IHistory source)
		{
			Ancestor = source;
			Id = source.Id;
			Type = source.Type;
			Transition = source.Transition;
		}

		bool IVisitorEntity<History, IHistory>.RefEquals(in History other) =>
				Type == other.Type &&
				ReferenceEquals(Id, other.Id) &&
				ReferenceEquals(Transition, other.Transition);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;

		FormattableString IDebugEntityId.EntityId => $"{Id}";
	}
}