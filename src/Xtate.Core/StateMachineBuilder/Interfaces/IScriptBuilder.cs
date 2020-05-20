namespace Xtate
{
	public interface IScriptBuilder
	{
		IScript Build();

		void SetSource(IExternalScriptExpression source);
		void SetBody(IScriptExpression content);
	}
}