using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IExecutionContext
	{
		IContextItems RuntimeItems { get; }

		DataModelObject DataModel { get; }

		bool InState(IIdentifier id);

		ValueTask Cancel(string sendId, CancellationToken token = default);

		ValueTask Log(string label, DataModelValue arguments = default, CancellationToken token = default);

		ValueTask Send(IOutgoingEvent @event, CancellationToken token = default);

		ValueTask StartInvoke(InvokeData invokeData, CancellationToken token = default);

		ValueTask CancelInvoke(string invokeId, CancellationToken token = default);
	}

	public interface IContextItems
	{
		object this[object key] { get; set; }
	}
}