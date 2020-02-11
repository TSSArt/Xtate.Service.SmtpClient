using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	internal sealed class ElseIfNode : IElseIf, IAncestorProvider, IStoreSupport, IDocumentId, IDebugEntityId
	{
		private readonly LinkedListNode<int> _documentIdNode;
		private readonly ElseIf              _entity;

		public ElseIfNode(LinkedListNode<int> documentIdNode, in ElseIf entity)
		{
			_documentIdNode = documentIdNode;
			_entity = entity;
		}

		object IAncestorProvider.Ancestor => _entity.Ancestor;

		FormattableString IDebugEntityId.EntityId => $"(#{DocumentId})";

		public int DocumentId => _documentIdNode.Value;

		public IConditionExpression Condition => _entity.Condition;

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.ElseIfNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.AddEntity(Key.Condition, Condition);
		}
	}
}