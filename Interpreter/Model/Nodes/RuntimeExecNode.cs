using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class RuntimeExecNode : IExecutableEntity, IStoreSupport, IAncestorProvider, IDocumentId
	{
		private readonly LinkedListNode<int> _documentIdNode;
		private readonly IExecutableEntity   _entity;

		public RuntimeExecNode(LinkedListNode<int> documentIdNode, IExecutableEntity entity)
		{
			_entity = entity;
			_documentIdNode = documentIdNode;
		}

		public object Ancestor => _entity;

		public int DocumentId => _documentIdNode.Value;

		public void Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.RuntimeExecNode);
			bucket.Add(Key.DocumentId, DocumentId);
		}
	}
}