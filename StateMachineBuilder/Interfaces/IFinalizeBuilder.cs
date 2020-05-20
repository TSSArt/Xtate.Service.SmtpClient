namespace Xtate
{
	public interface IFinalizeBuilder
	{
		IFinalize Build();

		void AddAction(IExecutableEntity action);
	}
}