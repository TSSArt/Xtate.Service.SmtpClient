using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IService
	{
		ValueTask Send(IEvent @event, CancellationToken token);
		ValueTask Destroy(CancellationToken token);
	}
}