using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public class DefaultLogEvaluator : ILog, IExecEvaluator, IAncestorProvider
	{
		private readonly LogEntity _log;

		public DefaultLogEvaluator(in LogEntity log)
		{
			_log = log;
			ExpressionEvaluator = log.Expression?.As<IObjectEvaluator>();
		}

		public IObjectEvaluator? ExpressionEvaluator { get; }

		object? IAncestorProvider.Ancestor => _log.Ancestor;

		public virtual async ValueTask Execute(IExecutionContext executionContext, CancellationToken token)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));

			var data = DataModelValue.Undefined;

			if (ExpressionEvaluator != null)
			{
				var obj = await ExpressionEvaluator.EvaluateObject(executionContext, token).ConfigureAwait(false);
				data = DataModelValue.FromObject(obj.ToObject()).DeepClone(true);
			}

			await executionContext.Log(_log.Label, data, token).ConfigureAwait(false);
		}

		public IValueExpression? Expression => _log.Expression;

		public string? Label => _log.Label;
	}
}