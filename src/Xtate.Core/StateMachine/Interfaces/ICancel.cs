namespace TSSArt.StateMachine
{
	public interface ICancel : IExecutableEntity
	{
		string?           SendId           { get; }
		IValueExpression? SendIdExpression { get; }
	}
}