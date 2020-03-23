using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public interface IServiceCommunication
	{
		ValueTask SendToCreator(IOutgoingEvent outgoingEvent, CancellationToken token = default);
	}
}