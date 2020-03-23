using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	internal sealed class ElseIfNode : IElseIf, IAncestorProvider, IStoreSupport, IDocumentId, IDebugEntityId
	{
		private readonly LinkedListNode<int> _documentIdNode;
		private readonly ElseIfEntity        _entity;

		public ElseIfNode(LinkedListNode<int> documentIdNode, in ElseIfEntity entity)
		{
			Infrastructure.Assert(entity.Condition != null);

			_documentIdNode = documentIdNode;
			_entity = entity;
		}

	#region Interface IAncestorProvider

		object? IAncestorProvider.Ancestor => _entity.Ancestor;

	#endregion

	#region Interface IDebugEntityId

		FormattableString IDebugEntityId.EntityId => @$"(#{DocumentId})";

	#endregion

	#region Interface IDocumentId

		public int DocumentId => _documentIdNode.Value;

	#endregion

	#region Interface IElseIf

		public IConditionExpression Condition => _entity.Condition!;

	#endregion

	#region Interface IStoreSupport

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.ElseIfNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.AddEntity(Key.Condition, Condition);
		}

	#endregion
	}
}