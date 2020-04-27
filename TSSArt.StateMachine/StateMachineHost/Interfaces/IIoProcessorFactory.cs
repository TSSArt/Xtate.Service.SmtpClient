using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IIoProcessorFactory
	{
		ValueTask<IIoProcessor> Create(IEventConsumer eventConsumer, CancellationToken token);
	}
}