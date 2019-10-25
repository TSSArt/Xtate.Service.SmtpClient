namespace TSSArt.StateMachine
{
	public interface IContent : IEntity
	{
		IValueExpression Expression { get; }
		string           Value      { get; }
	}
}