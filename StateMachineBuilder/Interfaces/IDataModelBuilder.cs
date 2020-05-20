namespace Xtate
{
	public interface IDataModelBuilder
	{
		IDataModel Build();

		void AddData(IData data);
	}
}