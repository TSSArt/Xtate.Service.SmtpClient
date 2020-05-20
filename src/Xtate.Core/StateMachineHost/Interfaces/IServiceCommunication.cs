using System.Threading;
using System.Threading.Tasks;
using Xtate.Annotations;

namespace Xtate
{
	[PublicAPI]
	public interface IServiceCommunication
	{
		ValueTask SendToCreator(IOutgoingEvent outgoingEvent, CancellationToken token = default);
	}
}