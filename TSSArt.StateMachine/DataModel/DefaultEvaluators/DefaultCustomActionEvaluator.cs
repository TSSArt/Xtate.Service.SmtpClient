using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class DefaultCustomActionEvaluator : ICustomAction, IExecEvaluator, IAncestorProvider, ICustomActionConsumer
	{
		private readonly CustomAction                                          _customAction;
		private          Func<IExecutionContext, CancellationToken, ValueTask> _action;

		public DefaultCustomActionEvaluator(in CustomAction customAction) => _customAction = customAction;

		object IAncestorProvider.Ancestor => _customAction.Ancestor;

		public string Xml => _customAction.Xml;

		public void SetAction(Func<IExecutionContext, CancellationToken, ValueTask> action) => _action = action;

		public virtual ValueTask Execute(IExecutionContext executionContext, CancellationToken token)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));

			if (_action == null)
			{
				throw new InvalidOperationException("Custom action does not configured");
			}

			return _action(executionContext, token);
		}
	}
}