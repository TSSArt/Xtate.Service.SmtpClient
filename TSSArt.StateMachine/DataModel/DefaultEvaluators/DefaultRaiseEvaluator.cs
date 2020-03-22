using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public class DefaultRaiseEvaluator : IRaise, IExecEvaluator, IAncestorProvider
	{
		private readonly RaiseEntity _raise;

		public DefaultRaiseEvaluator(in RaiseEntity raise)
		{
			Infrastructure.Assert(raise.OutgoingEvent != null);

			_raise = raise;
		}

		object? IAncestorProvider.Ancestor => _raise.Ancestor;

		public virtual ValueTask Execute(IExecutionContext executionContext, CancellationToken token)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));

			return executionContext.Send(_raise.OutgoingEvent!, token);
		}

		public IOutgoingEvent OutgoingEvent => _raise.OutgoingEvent!;
	}
}