using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class DefaultRaiseEvaluator : IRaise, IExecEvaluator, IAncestorProvider
	{
		private readonly Raise _raise;

		public DefaultRaiseEvaluator(in Raise raise) => _raise = raise;

		object IAncestorProvider.Ancestor => _raise.Ancestor;

		public virtual Task Execute(IExecutionContext executionContext, CancellationToken token)
		{
			return executionContext.Send(Event, type: null, EventTarget.InternalTarget, delayMs: 0, token);
		}

		public IEvent Event => _raise.Event;
	}
}