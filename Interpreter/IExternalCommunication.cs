using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IExternalCommunication
	{
		IReadOnlyList<IEventProcessor> GetIoProcessors(string sessionId);

		Task StartInvoke(string sessionId, string invokeId, Uri type, Uri source, DataModelValue data, CancellationToken token);
		Task CancelInvoke(string sessionId, string invokeId, CancellationToken token);
		Task SendEvent(string sessionId, IEvent @event, Uri type, Uri target, int delayMs, CancellationToken token);
		Task ForwardEvent(string sessionId, IEvent @event, string invokeId, CancellationToken token);
		Task CancelEvent(string sessionId, string sendId, CancellationToken token);
		Task ReturnDoneEvent(string sessionId, DataModelValue doneData, CancellationToken token);
	}
}