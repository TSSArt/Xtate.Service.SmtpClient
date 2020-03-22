using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	internal sealed class HistoryNode : StateEntityNode, IHistory, IAncestorProvider, IDebugEntityId
	{
		private readonly HistoryEntity _history;

		public HistoryNode(LinkedListNode<int> documentIdNode, in HistoryEntity history) : base(documentIdNode, children: null)
		{
			Infrastructure.Assert(history.Transition != null);

			_history = history;

			Id = history.Id ?? new IdentifierNode(new RuntimeIdentifier());
			Transition = history.Transition.As<TransitionNode>();
			Transition.SetSource(this);
		}

		public TransitionNode Transition { get; }

		object? IAncestorProvider.Ancestor => _history.Ancestor;

		FormattableString IDebugEntityId.EntityId => @$"{Id}(#{DocumentId})";

		ITransition IHistory.Transition => _history.Transition!;

		public override IIdentifier Id   { get; }
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