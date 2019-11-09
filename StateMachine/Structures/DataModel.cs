using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public struct DataModel : IDataModel, IEntity<DataModel, IDataModel>, IAncestorProvider
	{
		public IReadOnlyList<IData> Data { get; set; }

		void IEntity<DataModel, IDataModel>.Init(IDataModel source)
		{
			Ancestor = source;
			Data = source.Data;
		}

		bool IEntity<DataModel, IDataModel>.RefEquals(in DataModel other) => ReferenceEquals(Data, other.Data);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;
	}
}