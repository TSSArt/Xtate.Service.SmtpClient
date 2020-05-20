using System.Threading;
using System.Threading.Tasks;

namespace Xtate
{
	public interface IIoProcessorFactory
	{
		ValueTask<IIoProcessor> Create(IEventConsumer eventConsumer, CancellationToken token);
	}
}