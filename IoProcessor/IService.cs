using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IService
	{
		Task Send(IEvent @event, CancellationToken token);
		Task Destroy(CancellationToken token);
	}
}