using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IEventProcessor
	{
		Uri  Id      { get; }
		Uri? AliasId { get; }

		Uri GetTarget(string sessionId);

		ValueTask Dispatch(string sessionId, IOutgoingEvent evt, CancellationToken token);
	}
}