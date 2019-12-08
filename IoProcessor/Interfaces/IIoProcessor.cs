using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal interface IIoProcessor
	{
		IReadOnlyList<IEventProcessor> GetIoProcessors();
		ValueTask<SendStatus>          DispatchEvent(string sessionId, IOutgoingEvent @event, CancellationToken token);

		ValueTask StartInvoke(string sessionId, string invokeId, string invokeUniqueId, Uri type, Uri source, DataModelValue content, DataModelValue parameters,
							  CancellationToken token);

		ValueTask CancelInvoke(string sessionId, string invokeId, CancellationToken token);
		bool      IsInvokeActive(string sessionId, string invokeId, string invokeUniqueId);
		ValueTask ForwardEvent(string sessionId, IEvent @event, string invokeId, CancellationToken token);
	}
}