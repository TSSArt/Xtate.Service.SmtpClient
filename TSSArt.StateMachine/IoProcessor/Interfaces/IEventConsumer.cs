using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IEventConsumer
	{
		ValueTask Dispatch(string sessionId, IEvent @event, CancellationToken token);
	}
}