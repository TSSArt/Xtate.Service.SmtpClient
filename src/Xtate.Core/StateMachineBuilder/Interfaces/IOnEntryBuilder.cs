namespace Xtate
{
	public interface IOnEntryBuilder
	{
		IOnEntry Build();

		void AddAction(IExecutableEntity action);
	}
}