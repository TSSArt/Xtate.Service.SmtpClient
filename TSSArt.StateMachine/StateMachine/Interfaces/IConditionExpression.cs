namespace TSSArt.StateMachine
{
	public interface IConditionExpression : IExecutableEntity
	{
		string Expression { get; }
	}
}