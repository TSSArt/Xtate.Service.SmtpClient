using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IEventProcessor
	{
		Uri  Id      { get; }
		Uri  AliasId { get; }
		Uri  GetLocation(string sessionId);
		ValueTask Send(IEvent @event, Uri target, CancellationToken token);
	}
}