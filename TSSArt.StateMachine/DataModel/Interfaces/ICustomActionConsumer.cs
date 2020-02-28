namespace TSSArt.StateMachine
{
	public interface ICustomActionConsumer
	{
		void SetExecutor(ICustomActionExecutor executor);
	}
}