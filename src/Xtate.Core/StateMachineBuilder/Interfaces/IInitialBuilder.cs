namespace Xtate
{
	public interface IInitialBuilder
	{
		IInitial Build();

		void SetTransition(ITransition transition);
	}
}