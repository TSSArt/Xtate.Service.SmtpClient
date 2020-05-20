using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IEventConsumer
	{
		ValueTask Dispatch(SessionId sessionId, IEvent evt, CancellationToken token);
	}
}