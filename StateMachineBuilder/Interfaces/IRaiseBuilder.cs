namespace Xtate
{
	public interface IRaiseBuilder
	{
		IRaise Build();

		void SetEvent(IOutgoingEvent evt);
	}
}