using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal interface IIoProcessor
	{
		ImmutableArray<IEventProcessor> GetIoProcessors();
		ValueTask<SendStatus>           DispatchEvent(string sessionId, IOutgoingEvent @event, bool skipDelay, CancellationToken token);
		ValueTask                       StartInvoke(string sessionId, InvokeData invokeData, CancellationToken token);
		ValueTask                       CancelInvoke(string sessionId, string invokeId, CancellationToken token);
		bool                            IsInvokeActive(string sessionId, string invokeId, string invokeUniqueId);
		ValueTask                       ForwardEvent(string sessionId, IEvent @event, string invokeId, CancellationToken token);
	}
}