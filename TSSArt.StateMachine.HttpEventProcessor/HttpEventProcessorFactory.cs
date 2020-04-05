using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public sealed class HttpEventProcessorFactory : IEventProcessorFactory
	{
		private readonly Uri    _baseUri;
		private readonly string _path;

		public HttpEventProcessorFactory(Uri baseUri, string path)
		{
			_baseUri = baseUri;
			_path = path;
		}

	#region Interface IEventProcessorFactory

		public async ValueTask<IEventProcessor> Create(IEventConsumer eventConsumer, CancellationToken token)
		{
			var eventProcessor = new HttpEventProcessor(eventConsumer, _baseUri, _path);

			await eventProcessor.Start(token).ConfigureAwait(false);

			return eventProcessor;
		}

	#endregion
	}
}