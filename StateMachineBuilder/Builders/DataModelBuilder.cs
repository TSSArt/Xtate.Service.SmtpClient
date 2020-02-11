using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class DataModelBuilder : IDataModelBuilder
	{
		private ImmutableArray<IData>.Builder _dataList;

		public IDataModel Build() => new DataModel { Data = _dataList?.ToImmutable() ?? default };

		public void AddData(IData data)
		{
			if (data == null) throw new ArgumentNullException(nameof(data));

			(_dataList ??= ImmutableArray.CreateBuilder<IData>()).Add(data);
		}
	}
}