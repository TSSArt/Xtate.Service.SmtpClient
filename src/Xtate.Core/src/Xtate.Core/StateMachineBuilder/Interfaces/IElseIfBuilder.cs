namespace TSSArt.StateMachine
{
	public interface IElseIfBuilder
	{
		IElseIf Build();

		void SetCondition(IConditionExpression condition);
	}
}