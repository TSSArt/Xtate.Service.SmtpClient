namespace TSSArt.StateMachine
{
	public interface IDataModelHandlerFactory
	{
		bool CanHandle(string dataModelType);

		IDataModelHandler CreateHandler(IErrorProcessor errorProcessor);
	}
}