using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class OnEntryNode : IOnEntry, IStoreSupport, IAncestorProvider, IDocumentId, IDebugEntityId
	{
		private readonly LinkedListNode<int> _documentIdNode;
		private readonly OnEntry             _onEntry;

		public OnEntryNode(LinkedListNode<int> documentIdNode, in OnEntry onEntry)
		{
			_onEntry = onEntry;
			_documentIdNode = documentIdNode;
			ActionEvaluators = onEntry.Action.AsListOf<IExecEvaluator>();
		}

		public ImmutableArray<IExecEvaluator> ActionEvaluators { get; }

		object IAncestorProvider.Ancestor => _onEntry.Ancestor;

		FormattableString IDebugEntityId.EntityId => $"(#{DocumentId})";

		public int DocumentId => _documentIdNode.Value;

		public ImmutableArray<IExecutableEntity> Action => _onEntry.Action;

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.OnEntryNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.AddEntityList(Key.Action, Action);
		}
	}
}