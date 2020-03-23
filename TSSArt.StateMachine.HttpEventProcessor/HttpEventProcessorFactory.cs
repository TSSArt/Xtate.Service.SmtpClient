using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

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
			IWebHost webHost = null!;

			// ReSharper disable once PossibleNullReferenceException
			// ReSharper disable once AccessToModifiedClosure
			var eventProcessor = new HttpEventProcessor(eventConsumer, _baseUri, _path, () => new ValueTask(webHost!.StopAsync(CancellationToken.None)));

			webHost = new WebHostBuilder()
					  .Configure(builder => builder.Run(context => eventProcessor.Handle(context.Request).AsTask()))
					  .UseKestrel(serverOptions => serverOptions.ListenAnyIP(_baseUri.Port))
					  .Build();

			await webHost.StartAsync(token).ConfigureAwait(false);

			return eventProcessor;
		}

	#endregion
	}
}