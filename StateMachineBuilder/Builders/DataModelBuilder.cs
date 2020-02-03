using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class DataModelBuilder : IDataModelBuilder
	{
		private readonly List<IData> _dataList = new List<IData>();

		public IDataModel Build() => new DataModel { Data = DataList.Create(_dataList) };

		public void AddData(IData data)
		{
			if (data == null) throw new ArgumentNullException(nameof(data));
			_dataList.Add(data);
		}
	}
}