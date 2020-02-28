using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class DefaultCustomActionEvaluator : ICustomAction, IExecEvaluator, IAncestorProvider, ICustomActionConsumer
	{
		private readonly CustomAction          _customAction;
		private          ICustomActionExecutor _executor;

		public DefaultCustomActionEvaluator(in CustomAction customAction) => _customAction = customAction;

		object IAncestorProvider.Ancestor => _customAction.Ancestor;

		public string Xml => _customAction.Xml;

		public void SetExecutor(ICustomActionExecutor executor) => _executor = executor;

		public virtual ValueTask Execute(IExecutionContext executionContext, CancellationToken token)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));

			if (_executor == null)
			{
				throw new InvalidOperationException("Custom action does not configured");
			}

			return _executor.Execute(executionContext, token);
		}
	}
}