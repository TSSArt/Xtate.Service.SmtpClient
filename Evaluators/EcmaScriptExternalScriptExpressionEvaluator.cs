using System;
using System.Threading;
using System.Threading.Tasks;
using Jint.Parser;
using Jint.Parser.Ast;

namespace TSSArt.StateMachine.EcmaScript
{
	internal class EcmaScriptExternalScriptExpressionEvaluator : IExternalScriptExpression, IExecEvaluator, IExternalScriptConsumer, IAncestorProvider
	{
		private readonly ExternalScriptExpression _externalScriptExpression;
		private          Program                  _program;

		public EcmaScriptExternalScriptExpressionEvaluator(in ExternalScriptExpression externalScriptExpression) => _externalScriptExpression = externalScriptExpression;

		object IAncestorProvider.Ancestor => EcmaScriptHelper.GetAncestor(_externalScriptExpression);

		public Task Execute(IExecutionContext executionContext, CancellationToken token)
		{
			if (_program == null)
			{
				throw new InvalidOperationException(message: "External script missed.");
			}

			executionContext.Engine().Exec(_program, startNewScope: true);
			return Task.CompletedTask;
		}

		public void SetContent(string content)
		{
			if (content == null) throw new ArgumentNullException(nameof(content));

			_program = new JavaScriptParser().Parse(content);
		}

		public Uri Uri => _externalScriptExpression.Uri;
	}
}