using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IIoProcessor
	{
		Uri Id { get; }

		bool CanHandle(Uri? type, Uri? target);

		Uri GetTarget(string sessionId);

		ValueTask Dispatch(string sessionId, IOutgoingEvent evt, CancellationToken token);
	}
}