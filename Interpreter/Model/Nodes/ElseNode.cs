using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public class ElseNode : IElse, IStoreSupport, IAncestorProvider, IDocumentId, IDebugEntityId
	{
		private readonly LinkedListNode<int> _documentIdNode;
		private readonly Else                _entity;

		public ElseNode(LinkedListNode<int> documentIdNode, in Else entity)
		{
			_documentIdNode = documentIdNode;
			_entity = entity;
		}

		object IAncestorProvider.Ancestor => _entity.Ancestor;

		FormattableString IDebugEntityId.EntityId => $"(#{DocumentId})";

		public int DocumentId => _documentIdNode.Value;

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.ElseNode);
			bucket.Add(Key.DocumentId, DocumentId);
		}
	}
}