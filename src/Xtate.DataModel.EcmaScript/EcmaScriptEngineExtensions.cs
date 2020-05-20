namespace TSSArt.StateMachine.EcmaScript
{
	internal static class EcmaScriptEngineExtensions
	{
		public static EcmaScriptEngine Engine(this IExecutionContext executionContext) => EcmaScriptEngine.GetEngine(executionContext);
	}
}