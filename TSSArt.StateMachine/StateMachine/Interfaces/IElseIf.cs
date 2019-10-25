namespace TSSArt.StateMachine
{
	public interface IElseIf : IExecutableEntity
	{
		IConditionExpression Condition { get; }
	}
}