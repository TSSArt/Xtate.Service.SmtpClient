namespace TSSArt.StateMachine
{
	public interface IScript : IExecutableEntity
	{
		IScriptExpression         Content { get; }
		IExternalScriptExpression Source  { get; }
	}
}