using System;
using System.Collections./**/Immutable;

namespace TSSArt.StateMachine
{
	public class HistoryNode : StateEntityNode, IHistory, IAncestorProvider, IDebugEntityId
	{
		private readonly History _history;

		public HistoryNode(LinkedListNode<int> documentIdNode, in History history) : base(documentIdNode, children: null)
		{
			_history = history;

			Transition = history.Transition.As<TransitionNode>();

			Transition.SetSource(this);
		}

		public TransitionNode Transition { get; }

		object IAncestorProvider.Ancestor => _history.Ancestor;

		FormattableString IDebugEntityId.EntityId => $"{Id}(#{DocumentId})";

		ITransition IHistory.Transition => _history.Transition;

		public override IIdentifier Id   => _history.Id;
		public          HistoryType Type => _history.Type;

		protected override void Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.HistoryNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.Add(Key.HistoryType, Type);
			bucket.AddEntity(Key.Id, Id);
			bucket.AddEntity(Key.Transition, Transition);
		}
	}
}