using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Xtate.IoProcessor
{
	public sealed class HttpIoProcessorFactory : IIoProcessorFactory
	{
		private readonly Uri        _baseUri;
		private readonly IPEndPoint _ipEndPoint;

		public HttpIoProcessorFactory(Uri baseUri, IPEndPoint ipEndPoint)
		{
			_baseUri = baseUri;
			_ipEndPoint = ipEndPoint;
		}

	#region Interface IIoProcessorFactory

		public async ValueTask<IIoProcessor> Create(IEventConsumer eventConsumer, CancellationToken token)
		{
			var httpIoProcessor = new HttpIoProcessor(eventConsumer, _baseUri, _ipEndPoint);

			await httpIoProcessor.Start(token).ConfigureAwait(false);

			return httpIoProcessor;
		}

	#endregion
	}
}