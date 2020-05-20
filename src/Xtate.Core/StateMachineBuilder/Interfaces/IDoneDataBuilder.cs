namespace Xtate
{
	public interface IDoneDataBuilder
	{
		IDoneData Build();

		void SetContent(IContent content);
		void AddParameter(IParam parameter);
	}
}