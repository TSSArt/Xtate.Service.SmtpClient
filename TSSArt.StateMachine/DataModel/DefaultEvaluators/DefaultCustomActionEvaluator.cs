using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public class DefaultCustomActionEvaluator : ICustomAction, IExecEvaluator, IAncestorProvider, ICustomActionConsumer
	{
		private readonly CustomAction           _customAction;
		private          ICustomActionExecutor? _executor;

		public DefaultCustomActionEvaluator(in CustomAction customAction)
		{
			Infrastructure.Assert(customAction.Xml != null);

			_customAction = customAction;
		}

	#region Interface IAncestorProvider

		object? IAncestorProvider.Ancestor => _customAction.Ancestor;

	#endregion

	#region Interface ICustomAction

		public string Xml => _customAction.Xml!;

	#endregion

	#region Interface ICustomActionConsumer

		public void SetExecutor(ICustomActionExecutor executor) => _executor = executor;

	#endregion

	#region Interface IExecEvaluator

		public virtual ValueTask Execute(IExecutionContext executionContext, CancellationToken token)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));

			Infrastructure.Assert(_executor != null, Resources.Assertion_Custom_action_does_not_configured);

			return _executor.Execute(executionContext, token);
		}

	#endregion
	}
}