using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IEventProcessorFactory
	{
		ValueTask<IEventProcessor> Create(IEventConsumer eventConsumer, CancellationToken token);
	}
}