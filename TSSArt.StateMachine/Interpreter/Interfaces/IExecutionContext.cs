using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IExecutionContext
	{
		IContextItems RuntimeItems { get; }

		DataModelObject DataModel { get; }

		bool InState(IIdentifier id);

		ValueTask Cancel(string sendId, CancellationToken token);

		ValueTask Log(string label, DataModelValue arguments, CancellationToken token);

		ValueTask Send(IOutgoingEvent @event, CancellationToken token);

		ValueTask StartInvoke(string invokeId, Uri type, Uri source, DataModelValue content, DataModelValue parameters, CancellationToken token);

		ValueTask CancelInvoke(string invokeId, CancellationToken token);
	}

	public interface IContextItems
	{
		object this[object key] { get; set; }
	}
}