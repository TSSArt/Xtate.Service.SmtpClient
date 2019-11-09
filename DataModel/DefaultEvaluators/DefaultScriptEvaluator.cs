using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class DefaultScriptEvaluator : IScript, IExecEvaluator, IAncestorProvider
	{
		private readonly Script _script;

		public DefaultScriptEvaluator(in Script script)
		{
			_script = script;
			ContentEvaluator = script.Content.As<IExecEvaluator>();
			SourceEvaluator = script.Source.As<IExecEvaluator>();
		}

		public IExecEvaluator ContentEvaluator { get; }
		public IExecEvaluator SourceEvaluator  { get; }

		object IAncestorProvider.Ancestor => _script.Ancestor;

		public virtual ValueTask Execute(IExecutionContext executionContext, CancellationToken token)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));

			return (ContentEvaluator ?? SourceEvaluator).Execute(executionContext, token);
		}

		public IScriptExpression Content => _script.Content;

		public IExternalScriptExpression Source => _script.Source;
	}
}