namespace Xtate
{
	public interface IElseIfBuilder
	{
		IElseIf Build();

		void SetCondition(IConditionExpression condition);
	}
}