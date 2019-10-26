using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class DefaultRaiseEvaluator : IRaise, IExecEvaluator, IAncestorProvider
	{
		private readonly Raise _raise;

		public DefaultRaiseEvaluator(in Raise raise) => _raise = raise;

		object IAncestorProvider.Ancestor => _raise.Ancestor;

		public virtual ValueTask Execute(IExecutionContext executionContext, CancellationToken token) => executionContext.Send(Event, type: null, target: EventTarget.InternalTarget, delayMs: 0, token: token);

		public IEvent Event => _raise.Event;
	}
}