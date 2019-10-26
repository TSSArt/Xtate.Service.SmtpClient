using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IExternalCommunication
	{
		IReadOnlyList<IEventProcessor> GetIoProcessors(string sessionId);

		ValueTask StartInvoke(string sessionId, string invokeId, Uri type, Uri source, DataModelValue data, CancellationToken token);
		ValueTask CancelInvoke(string sessionId, string invokeId, CancellationToken token);
		ValueTask SendEvent(string sessionId, IEvent @event, Uri type, Uri target, int delayMs, CancellationToken token);
		ValueTask ForwardEvent(string sessionId, IEvent @event, string invokeId, CancellationToken token);
		ValueTask CancelEvent(string sessionId, string sendId, CancellationToken token);
		ValueTask ReturnDoneEvent(string sessionId, DataModelValue doneData, CancellationToken token);
	}
}