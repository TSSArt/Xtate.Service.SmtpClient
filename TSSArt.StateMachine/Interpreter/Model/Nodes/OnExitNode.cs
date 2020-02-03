using System;
using System.Collections./**/Immutable;

namespace TSSArt.StateMachine
{
	public class OnExitNode : IOnExit, IStoreSupport, IAncestorProvider, IDocumentId, IDebugEntityId
	{
		private readonly LinkedListNode<int> _documentIdNode;
		private readonly OnExit              _onExit;

		public OnExitNode(LinkedListNode<int> documentIdNode, in OnExit onExit)
		{
			_onExit = onExit;
			_documentIdNode = documentIdNode;
			ActionEvaluators = onExit.Action.AsListOf<IExecEvaluator>();
		}

		public /**/ImmutableArray<IExecEvaluator> ActionEvaluators { get; }

		object IAncestorProvider.Ancestor => _onExit.Ancestor;

		FormattableString IDebugEntityId.EntityId => $"(#{DocumentId})";

		public int DocumentId => _documentIdNode.Value;

		public /**/ImmutableArray<IExecutableEntity> Action => _onExit.Action;

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.OnExitNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.AddEntityList(Key.Action, Action);
		}
	}
}