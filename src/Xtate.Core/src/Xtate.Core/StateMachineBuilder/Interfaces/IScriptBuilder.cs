namespace TSSArt.StateMachine
{
	public interface IScriptBuilder
	{
		IScript Build();

		void SetSource(IExternalScriptExpression source);
		void SetBody(IScriptExpression content);
	}
}