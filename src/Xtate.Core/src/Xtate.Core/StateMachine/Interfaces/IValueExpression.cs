namespace TSSArt.StateMachine
{
	public interface IValueExpression : IExecutableEntity
	{
		string? Expression { get; }
	}
}