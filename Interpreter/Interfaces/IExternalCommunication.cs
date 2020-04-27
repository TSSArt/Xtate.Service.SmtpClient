using System;
using System.Collections.Immutable;
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

	public class InvokeData
	{
		public InvokeData(string invokeId, string invokeUniqueId, Uri type, Uri? source, string? rawContent, DataModelValue content, DataModelValue parameters)
		{
			InvokeId = invokeId;
			InvokeUniqueId = invokeUniqueId;
			Type = type;
			Source = source;
			RawContent = rawContent;
			Content = content;
			Parameters = parameters;
		}

		public string         InvokeId       { get; }
		public string         InvokeUniqueId { get; }
		public Uri            Type           { get; }
		public Uri?           Source         { get; }
		public string?        RawContent     { get; }
		public DataModelValue Content        { get; }
		public DataModelValue Parameters     { get; }
	}

	public interface IExternalCommunication
	{
		ImmutableArray<IIoProcessor> GetIoProcessors();

		ValueTask             StartInvoke(InvokeData invokeData, CancellationToken token);
		ValueTask             CancelInvoke(string invokeId, CancellationToken token);
		bool                  IsInvokeActive(string invokeId, string invokeUniqueId);
		ValueTask<SendStatus> TrySendEvent(IOutgoingEvent evt, CancellationToken token);
		ValueTask             ForwardEvent(IEvent evt, string invokeId, CancellationToken token);
		ValueTask             CancelEvent(string sendId, CancellationToken token);
	}
}