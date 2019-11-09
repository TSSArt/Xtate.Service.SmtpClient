using System;
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

		public virtual async ValueTask Execute(IExecutionContext executionContext, CancellationToken token)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));

			DataModelValue data;
			if (ExpressionEvaluator != null)
			{
				var obj = await ExpressionEvaluator.EvaluateObject(executionContext, token).ConfigureAwait(false);
				data = DataModelValue.FromObject(obj.ToObject()).DeepClone(true);
			}
			else
			{
				data = DataModelValue.Undefined(true);
			}

			await executionContext.Log(_log.Label, data, token).ConfigureAwait(false);
		}

		public IValueExpression Expression => _log.Expression;

		public string Label => _log.Label;
	}
}