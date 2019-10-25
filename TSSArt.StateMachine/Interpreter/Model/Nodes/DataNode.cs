using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public class DataNode : IData, IStoreSupport, IAncestorProvider, IDocumentId, IDebugEntityId
	{
		private readonly Data                _data;
		private readonly LinkedListNode<int> _documentIdNode;

		public DataNode(LinkedListNode<int> documentIdNode, in Data data)
		{
			_documentIdNode = documentIdNode;
			_data = data;
			ExpressionEvaluator = data.Expression.As<IObjectEvaluator>();
		}

		public IObjectEvaluator ExpressionEvaluator { get; }

		object IAncestorProvider.Ancestor => _data.Ancestor;

		public IValueExpression        Expression    => _data.Expression;
		public IExternalDataExpression Source        => _data.Source;
		public string                  Id            => _data.Id;
		public string                  InlineContent => _data.InlineContent;

		FormattableString IDebugEntityId.EntityId => $"{Id}(#{DocumentId})";

		public int DocumentId => _documentIdNode.Value;

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.DataNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.Add(Key.Id, Id);
			bucket.AddEntity(Key.Source, Source);
			bucket.AddEntity(Key.Expression, Expression);
			bucket.Add(Key.InlineContent, InlineContent);
		}
	}
}