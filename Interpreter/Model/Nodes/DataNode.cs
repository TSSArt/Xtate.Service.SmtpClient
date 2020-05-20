using System;

namespace Xtate
{
	internal sealed class DataNode : IData, IStoreSupport, IAncestorProvider, IDocumentId, IDebugEntityId
	{
		private readonly DataEntity       _data;
		private          DocumentIdRecord _documentIdNode;

		public DataNode(in DocumentIdRecord documentIdNode, in DataEntity data)
		{
			Infrastructure.Assert(data.Id != null);

			_documentIdNode = documentIdNode;
			_data = data;

			ExpressionEvaluator = data.Expression?.As<IObjectEvaluator>();
		}

		public IObjectEvaluator? ExpressionEvaluator { get; }

	#region Interface IAncestorProvider

		object? IAncestorProvider.Ancestor => _data.Ancestor;

	#endregion

	#region Interface IData

		public IValueExpression?        Expression    => _data.Expression;
		public IExternalDataExpression? Source        => _data.Source;
		public string                   Id            => _data.Id!;
		public string?                  InlineContent => _data.InlineContent;

	#endregion

	#region Interface IDebugEntityId

		FormattableString IDebugEntityId.EntityId => @$"{Id}(#{DocumentId})";

	#endregion

	#region Interface IDocumentId

		public int DocumentId => _documentIdNode.Value;

	#endregion

	#region Interface IStoreSupport

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.DataNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.Add(Key.Id, Id);
			bucket.AddEntity(Key.Source, Source);
			bucket.AddEntity(Key.Expression, Expression);
			bucket.Add(Key.InlineContent, InlineContent);
		}

	#endregion
	}
}