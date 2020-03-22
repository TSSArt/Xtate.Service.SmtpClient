using System.Threading;
using System.Threading.Tasks;
using Jint.Parser.Ast;

namespace TSSArt.StateMachine.EcmaScript
{
	internal class EcmaScriptScriptExpressionEvaluator : IScriptExpression, IExecEvaluator, IAncestorProvider
	{
		private readonly Program           _program;
		private readonly IScriptExpression _scriptExpression;

		public EcmaScriptScriptExpressionEvaluator(IScriptExpression scriptExpression, Program program)
		{
			_scriptExpression = scriptExpression;
			_program = program;
		}

		object? IAncestorProvider.Ancestor => _scriptExpression;

		public ValueTask Execute(IExecutionContext executionContext, CancellationToken token)
		{
			executionContext.Engine().Exec(_program, startNewScope: true);

			return default;
		}

		public string? Expression => _scriptExpression.Expression;
	}
}