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

		ValueTask Log(string label, object arguments, CancellationToken token);

		ValueTask Send(IEvent @event, Uri type, Uri target, int delayMs, CancellationToken token);
	}

	public interface IContextItems
	{
		object this[object key] { get; set; }
	}
}