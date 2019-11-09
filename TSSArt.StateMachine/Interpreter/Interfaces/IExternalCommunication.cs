using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public enum SendStatus
	{
		Sent,
		ToSchedule,
		ToInternalQueue
	}

	public interface IExternalCommunication
	{
		IReadOnlyList<IEventProcessor> GetIoProcessors();

		ValueTask             StartInvoke(string invokeId, Uri type, Uri source, DataModelValue data, CancellationToken token);
		ValueTask             CancelInvoke(string invokeId, CancellationToken token);
		bool                  IsInvokeActive(string invokeId);
		ValueTask<SendStatus> TrySendEvent(IOutgoingEvent @event, CancellationToken token);
		ValueTask             ForwardEvent(IEvent @event, string invokeId, CancellationToken token);
		ValueTask             CancelEvent(string sendId, CancellationToken token);
	}
}