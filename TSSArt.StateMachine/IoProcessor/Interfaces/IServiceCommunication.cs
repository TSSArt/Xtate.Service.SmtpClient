using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IServiceCommunication
	{
		ValueTask SendToCreator(IOutgoingEvent outgoingEvent, CancellationToken token);
	}
}