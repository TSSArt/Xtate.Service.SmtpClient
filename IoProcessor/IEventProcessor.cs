using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IEventProcessor
	{
		Uri Id      { get; }
		Uri AliasId { get; }
		Uri GetOrigin(string sessionId);

		ValueTask Dispatch(Uri origin, Uri originType, IOutgoingEvent @event, CancellationToken token);
	}
}