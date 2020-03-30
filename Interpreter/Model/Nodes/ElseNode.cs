using System;

namespace TSSArt.StateMachine
{
	internal sealed class ElseNode : IElse, IStoreSupport, IAncestorProvider, IDocumentId, IDebugEntityId
	{
		private          DocumentIdRecord _documentIdNode;
		private readonly ElseEntity       _entity;

		public ElseNode(in DocumentIdRecord documentIdNode, in ElseEntity entity)
		{
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

	#region Interface IStoreSupport

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.ElseNode);
			bucket.Add(Key.DocumentId, DocumentId);
		}

	#endregion
	}
}