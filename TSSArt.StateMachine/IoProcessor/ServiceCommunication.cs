using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal class ServiceCommunication : IServiceCommunication
	{
		private readonly string       _invokeId;
		private readonly IIoProcessor _ioProcessor;
		private readonly string       _sessionId;

		public ServiceCommunication(IIoProcessor ioProcessor, string sessionId, string invokeId)
		{
			_ioProcessor = ioProcessor;
			_sessionId = sessionId;
			_invokeId = invokeId;
		}

		public ValueTask SendToCreator(IOutgoingEvent outgoingEvent, CancellationToken token) => _ioProcessor.DispatchServiceEvent(_sessionId, _invokeId, outgoingEvent, token);
	}
}