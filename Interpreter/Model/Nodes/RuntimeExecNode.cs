using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	internal sealed class RuntimeExecNode : IExecutableEntity, IStoreSupport, IAncestorProvider, IDocumentId
	{
		private readonly LinkedListNode<int> _documentIdNode;
		private readonly IExecutableEntity   _entity;

		public RuntimeExecNode(LinkedListNode<int> documentIdNode, IExecutableEntity entity)
		{
			_entity = entity;
			_documentIdNode = documentIdNode;
		}

	#region Interface IAncestorProvider

		public object Ancestor => _entity;

	#endregion

	#region Interface IDocumentId

		public int DocumentId => _documentIdNode.Value;

	#endregion

	#region Interface IStoreSupport

		public void Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.RuntimeExecNode);
			bucket.Add(Key.DocumentId, DocumentId);
		}

	#endregion
	}
}