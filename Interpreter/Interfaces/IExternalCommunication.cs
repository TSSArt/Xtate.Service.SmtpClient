using System;
using System.Collections./**/Immutable;
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

	public struct InvokeData
	{
		public string         InvokeId       { get; set; }
		public string         InvokeUniqueId { get; set; }
		public Uri            Type           { get; set; }
		public Uri            Source         { get; set; }
		public string         RawContent     { get; set; }
		public DataModelValue Content        { get; set; }
		public DataModelValue Parameters     { get; set; }
	}

	public interface IExternalCommunication
	{
		/**/ImmutableArray<IEventProcessor> GetIoProcessors();

		ValueTask             StartInvoke(InvokeData invokeData, CancellationToken token);
		ValueTask             CancelInvoke(string invokeId, CancellationToken token);
		bool                  IsInvokeActive(string invokeId, string invokeUniqueId);
		ValueTask<SendStatus> TrySendEvent(IOutgoingEvent @event, CancellationToken token);
		ValueTask             ForwardEvent(IEvent @event, string invokeId, CancellationToken token);
		ValueTask             CancelEvent(string sendId, CancellationToken token);
	}
}