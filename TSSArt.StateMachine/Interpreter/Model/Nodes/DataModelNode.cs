using System.Collections.Generic;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	internal sealed class DataModelNode : IDataModel, IStoreSupport, IAncestorProvider, IDocumentId
	{
		private readonly DataModel           _dataModel;
		private readonly LinkedListNode<int> _documentIdNode;

		public DataModelNode(LinkedListNode<int> documentIdNode, in DataModel dataModel)
		{
			_documentIdNode = documentIdNode;
			_dataModel = dataModel;
			Data = dataModel.Data.AsArrayOf<IData, DataNode>(true);
		}

		public ImmutableArray<DataNode> Data { get; }

		object IAncestorProvider.Ancestor => _dataModel.Ancestor;

		ImmutableArray<IData> IDataModel.Data => _dataModel.Data;

		public int DocumentId => _documentIdNode.Value;

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.DataModelNode);
			bucket.AddEntityList(Key.DataList, Data);
		}
	}
}