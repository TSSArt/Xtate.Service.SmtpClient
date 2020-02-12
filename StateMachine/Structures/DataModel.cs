using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public struct DataModel : IDataModel, IVisitorEntity<DataModel, IDataModel>, IAncestorProvider
	{
		public ImmutableArray<IData> Data { get; set; }

		void IVisitorEntity<DataModel, IDataModel>.Init(IDataModel source)
		{
			Ancestor = source;
			Data = source.Data;
		}

		bool IVisitorEntity<DataModel, IDataModel>.RefEquals(in DataModel other) => Data == other.Data;

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}