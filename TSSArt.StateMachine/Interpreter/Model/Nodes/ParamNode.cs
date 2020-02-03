using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class ParamNode : IParam, IStoreSupport, IAncestorProvider, IDocumentId, IDebugEntityId
	{
		private readonly LinkedListNode<int> _documentIdNode;
		private readonly Param               _param;

		public ParamNode(LinkedListNode<int> documentIdNode, in Param param)
		{
			_documentIdNode = documentIdNode;
			_param = param;
		}

		object IAncestorProvider.Ancestor => _param.Ancestor;

		FormattableString IDebugEntityId.EntityId => $"{Name}(#{DocumentId})";

		public int DocumentId => _documentIdNode.Value;

		public string Name => _param.Name;

		public IValueExpression Expression => _param.Expression;

		public ILocationExpression Location => _param.Location;

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.ParamNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.Add(Key.Name, Name);
			bucket.AddEntity(Key.Expression, Expression);
			bucket.AddEntity(Key.Location, Location);
		}
	}
}