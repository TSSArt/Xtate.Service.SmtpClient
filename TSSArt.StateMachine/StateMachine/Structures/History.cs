using System;

namespace TSSArt.StateMachine
{
	public struct History : IHistory, IEntity<History, IHistory>, IAncestorProvider, IDebugEntityId
	{
		public IIdentifier Id;
		public HistoryType Type;
		public ITransition Transition;

		IIdentifier IHistory.Id => Id;

		HistoryType IHistory.Type => Type;

		ITransition IHistory.Transition => Transition;

		void IEntity<History, IHistory>.Init(IHistory source)
		{
			Ancestor = source;
			Id = source.Id;
			Type = source.Type;
			Transition = source.Transition;
		}

		bool IEntity<History, IHistory>.RefEquals(in History other) =>
				Type == other.Type &&
				ReferenceEquals(Id, other.Id) &&
				ReferenceEquals(Transition, other.Transition);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;

		FormattableString IDebugEntityId.EntityId => $"{Id}";
	}
}