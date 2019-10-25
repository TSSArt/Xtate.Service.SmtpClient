using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class DefaultLogEvaluator : ILog, IExecEvaluator, IAncestorProvider
	{
		private readonly Log _log;

		public DefaultLogEvaluator(in Log log)
		{
			_log = log;
			ExpressionEvaluator = log.Expression.As<IObjectEvaluator>();
		}

		public IObjectEvaluator ExpressionEvaluator { get; }

		object IAncestorProvider.Ancestor => _log.Ancestor;

		public virtual async Task Execute(IExecutionContext executionContext, CancellationToken token)
		{
			var arguments = ExpressionEvaluator != null ? (await ExpressionEvaluator.EvaluateObject(executionContext, token).ConfigureAwait(false)).ToObject() : null;

			await executionContext.Log(_log.Label, arguments, token).ConfigureAwait(false);
		}

		public IValueExpression Expression => _log.Expression;

		public string Label => _log.Label;
	}
}