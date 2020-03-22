using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public struct DataModelEntity : IDataModel, IVisitorEntity<DataModelEntity, IDataModel>, IAncestorProvider
	{
		public ImmutableArray<IData> Data { get; set; }

		void IVisitorEntity<DataModelEntity, IDataModel>.Init(IDataModel source)
		{
			Ancestor = source;
			Data = source.Data;
		}

		bool IVisitorEntity<DataModelEntity, IDataModel>.RefEquals(in DataModelEntity other) => Data == other.Data;

		internal object? Ancestor;

		object? IAncestorProvider.Ancestor => Ancestor;
	}
}