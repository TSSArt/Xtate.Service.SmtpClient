using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public class DefaultCancelEvaluator : ICancel, IExecEvaluator, IAncestorProvider
	{
		private readonly CancelEntity _cancel;

		public DefaultCancelEvaluator(in CancelEntity cancel)
		{
			_cancel = cancel;
			SendIdExpressionEvaluator = cancel.SendIdExpression?.As<IStringEvaluator>();
		}

		public IStringEvaluator? SendIdExpressionEvaluator { get; }

		object? IAncestorProvider.Ancestor => _cancel.Ancestor;

		public string?           SendId           => _cancel.SendId;
		public IValueExpression? SendIdExpression => _cancel.SendIdExpression;

		public virtual async ValueTask Execute(IExecutionContext executionContext, CancellationToken token)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));

			var sendId = SendIdExpressionEvaluator != null ? await SendIdExpressionEvaluator.EvaluateString(executionContext, token).ConfigureAwait(false) : SendId;

			if (string.IsNullOrEmpty(sendId))
			{
				throw new StateMachineExecutionException(Resources.Exception_SendIdIsEmpty);
			}

			await executionContext.Cancel(sendId, token).ConfigureAwait(false);
		}
	}
}