using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class DefaultCancelEvaluator : ICancel, IExecEvaluator, IAncestorProvider
	{
		private readonly Cancel _cancel;

		public DefaultCancelEvaluator(in Cancel cancel)
		{
			_cancel = cancel;
			SendIdExpressionEvaluator = cancel.SendIdExpression.As<IStringEvaluator>();
		}

		public IStringEvaluator SendIdExpressionEvaluator { get; }

		object IAncestorProvider.Ancestor => _cancel.Ancestor;

		public string           SendId           => _cancel.SendId;
		public IValueExpression SendIdExpression => _cancel.SendIdExpression;

		public virtual async Task Execute(IExecutionContext executionContext, CancellationToken token)
		{
			var sendId = SendIdExpressionEvaluator != null ? await SendIdExpressionEvaluator.EvaluateString(executionContext, token).ConfigureAwait(false) : SendId;

			await executionContext.Cancel(sendId, token).ConfigureAwait(false);
		}
	}
}