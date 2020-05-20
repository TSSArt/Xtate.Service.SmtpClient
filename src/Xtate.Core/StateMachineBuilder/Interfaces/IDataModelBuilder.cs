namespace TSSArt.StateMachine
{
	public interface IDataModelBuilder
	{
		IDataModel Build();

		void AddData(IData data);
	}
}