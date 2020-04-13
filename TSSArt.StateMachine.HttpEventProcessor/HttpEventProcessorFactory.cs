using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public sealed class HttpEventProcessorFactory : IEventProcessorFactory
	{
		private readonly Uri        _baseUri;
		private readonly IPEndPoint _ipEndPoint;

		public HttpEventProcessorFactory(Uri baseUri, IPEndPoint ipEndPoint)
		{
			_baseUri = baseUri;
			_ipEndPoint = ipEndPoint;
		}

	#region Interface IEventProcessorFactory

		public async ValueTask<IEventProcessor> Create(IEventConsumer eventConsumer, CancellationToken token)
		{
			var eventProcessor = new HttpEventProcessor(eventConsumer, _baseUri, _ipEndPoint);

			await eventProcessor.Start(token).ConfigureAwait(false);

			return eventProcessor;
		}

	#endregion
	}
}